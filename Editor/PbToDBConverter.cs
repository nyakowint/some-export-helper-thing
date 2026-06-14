using System.Collections.Generic;
using UnityEngine;
using VRC.Dynamics;

namespace Nyako.ExportHelper.Editor
{
    internal static class PbToDBConverter
    {
        /// <summary>
        /// Main entry point. Converts all VRCPhysBone components on the avatar to DynamicBone
        /// components, then destroys the originals. Also converts colliders first so the collider
        /// references can be wired up correctly.
        /// </summary>
        internal static void Convert(GameObject avatarRoot)
        {
            CheckSDKVersion();

            // Convert colliders first so we have the mapping ready
            var colliderMap = PbToDBColliderConverter.ConvertAll(avatarRoot);

            // Then convert all PhysBones
            var physBones = avatarRoot.GetComponentsInChildren<VRCPhysBoneBase>(true);
            foreach (var pb in physBones)
                ConvertPhysBone(pb, colliderMap);
        }

        private static void ConvertPhysBone(
            VRCPhysBoneBase pb,
            Dictionary<VRCPhysBoneColliderBase, DynamicBoneColliderBase> colliderMap)
        {
            if (pb.version == VRCPhysBoneBase.Version.Version_1_1)
            {
                Debug.LogWarning(
                    $"[LN Export Helper] {pb.name}: PhysBone Version 1.1 (SquishyBones) detected — " +
                    "stiffness and gravity values will be approximated.");
            }

            var rootBone = pb.rootTransform != null ? pb.rootTransform : pb.transform;

            // Gather chains based on multi-child type
            var chains = GetChainRoots(pb, rootBone);

            foreach (var chainRoot in chains)
                CreateDynamicBone(chainRoot, pb, colliderMap);

            // Warn for unsupported / dropped fields
            WarnDroppedFields(pb);

            Object.DestroyImmediate(pb);
        }

        private static List<Transform> GetChainRoots(VRCPhysBoneBase pb, Transform rootBone)
        {
            var chains = new List<Transform>();

            int childCount = rootBone.childCount;

            if (pb.multiChildType == VRCPhysBoneBase.MultiChildType.Ignore && childCount > 1)
            {
                // Create one DynamicBone per direct child branch
                for (int i = 0; i < childCount; i++)
                    chains.Add(rootBone.GetChild(i));

                Debug.LogWarning(
                    $"[LN Export Helper] {pb.name}: multi-child bone split into {childCount} separate " +
                    "DynamicBone components (one per direct child branch).");
            }
            else if (pb.multiChildType == VRCPhysBoneBase.MultiChildType.First && childCount > 0)
            {
                chains.Add(rootBone.GetChild(0));
            }
            else
            {
                // Average or single child — treat root as the single DB root
                chains.Add(rootBone);
            }

            return chains;
        }

        private static void CreateDynamicBone(
            Transform chainRoot,
            VRCPhysBoneBase pb,
            Dictionary<VRCPhysBoneColliderBase, DynamicBoneColliderBase> colliderMap)
        {
            var db = chainRoot.gameObject.AddComponent<DynamicBone>();

            db.m_Root = chainRoot;

            // --- Forces ---
            db.m_Elasticity = pb.pull;
            db.m_ElasticityDistrib = pb.pullCurve;

            // Spring (wobble) in PB is inverse of Damping in DB
            db.m_Damping = 1f - pb.spring;
            db.m_DampingDistrib = InvertCurve(pb.springCurve);

            db.m_Stiffness = pb.stiffness;
            db.m_StiffnessDistrib = pb.stiffnessCurve;

            // PB gravity is a float [-1,1]: positive = down, negative = up
            // DB m_Gravity is a world-space Vector3
            db.m_Gravity = pb.gravity * Vector3.down;

            // --- Immobile / Inert ---
            db.m_Inert = pb.immobile;
            db.m_InertDistrib = pb.immobileCurve;

            // --- Collision ---
            db.m_Radius = pb.radius;
            db.m_RadiusDistrib = pb.radiusCurve;

            // --- Endpoint ---
            if (pb.endpointPosition != Vector3.zero)
                db.m_EndOffset = pb.endpointPosition;

            // --- Exclusions ---
            if (pb.ignoreTransforms != null && pb.ignoreTransforms.Count > 0)
                db.m_Exclusions = new List<Transform>(pb.ignoreTransforms);

            // --- Colliders ---
            db.m_Colliders = BuildColliderList(pb.colliders, colliderMap);

            // --- Limits → FreezeAxis (lossy) ---
            if (pb.limitType != VRCPhysBoneBase.LimitType.None)
            {
                db.m_FreezeAxis = DynamicBone.FreezeAxis.None;
                Debug.LogWarning(
                    $"[LN Export Helper] {pb.name}: limitType '{pb.limitType}' has no DynamicBone equivalent — " +
                    "FreezeAxis set to None.");
            }
        }

        private static List<DynamicBoneColliderBase> BuildColliderList(
            List<VRCPhysBoneColliderBase> pbColliders,
            Dictionary<VRCPhysBoneColliderBase, DynamicBoneColliderBase> colliderMap)
        {
            if (pbColliders == null || pbColliders.Count == 0)
                return null;

            var result = new List<DynamicBoneColliderBase>(pbColliders.Count);
            foreach (var pbc in pbColliders)
            {
                if (pbc == null) continue;
                if (colliderMap.TryGetValue(pbc, out var dbc))
                    result.Add(dbc);
            }
            return result.Count > 0 ? result : null;
        }

        private static void WarnDroppedFields(VRCPhysBoneBase pb)
        {
            if (pb.gravityFalloff != 0f)
                Debug.LogWarning($"[LN Export Helper] {pb.name}: 'gravityFalloff' has no DynamicBone equivalent — dropped.");

            if (pb.isAnimated)
                Debug.LogWarning($"[LN Export Helper] {pb.name}: 'isAnimated' has no DynamicBone equivalent — dropped.");

            if (pb.maxStretch != 0f || pb.maxSquish != 0f || pb.stretchMotion != 0f)
                Debug.LogWarning($"[LN Export Helper] {pb.name}: SquishyBone stretch/squish parameters have no DynamicBone equivalent — dropped.");
        }

        /// <summary>
        /// Inverts the values of an AnimationCurve (new_value = 1 - old_value).
        /// Used to map PB spring (resistance) to DB damping (attenuation), which are inversely related.
        /// </summary>
        private static AnimationCurve InvertCurve(AnimationCurve src)
        {
            if (src == null || src.length == 0) return null;
            var keys = src.keys;
            for (int i = 0; i < keys.Length; i++)
                keys[i].value = 1f - keys[i].value;
            return new AnimationCurve(keys);
        }

        private static void CheckSDKVersion()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.vrchat.avatars");
            if (packageInfo == null)
            {
                Debug.LogError("[LN Export Helper] VRChat Avatars SDK package not found.");
                return;
            }

            if (System.Version.TryParse(packageInfo.version, out var ver) &&
                ver < new System.Version(3, 5, 0))
            {
                Debug.LogWarning(
                    $"[LN Export Helper] VRC SDK {packageInfo.version} is older than 3.5.0 — " +
                    "PhysBone fields may differ from expected.");
            }
        }
    }
}
