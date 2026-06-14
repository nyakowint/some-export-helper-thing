using System.Collections.Generic;
using UnityEngine;
using VRC.Dynamics;

namespace Nyako.ExportHelper.Editor
{
    internal static class PbToDBColliderConverter
    {
        /// <summary>
        /// Converts all VRCPhysBoneColliderBase components on the avatar to DynamicBoneCollider components.
        /// Returns a mapping from the original VRCPhysBoneColliderBase to its new DynamicBoneCollider so
        /// that bone converters can re-wire the collider lists.
        /// </summary>
        internal static Dictionary<VRCPhysBoneColliderBase, DynamicBoneColliderBase> ConvertAll(GameObject avatarRoot)
        {
            var map = new Dictionary<VRCPhysBoneColliderBase, DynamicBoneColliderBase>();
            var pbColliders = avatarRoot.GetComponentsInChildren<VRCPhysBoneColliderBase>(true);

            foreach (var pbc in pbColliders)
            {
                var dbc = ConvertCollider(pbc);
                if (dbc != null)
                    map[pbc] = dbc;
            }

            // Destroy originals after mapping is built
            foreach (var pbc in pbColliders)
                Object.DestroyImmediate(pbc);

            return map;
        }

        private static DynamicBoneColliderBase ConvertCollider(VRCPhysBoneColliderBase pbc)
        {
            switch (pbc.shapeType)
            {
                case VRCPhysBoneColliderBase.ShapeType.Plane:
                    Debug.LogWarning($"[BeatSaberExport] {pbc.name}: Plane collider has no DynamicBone equivalent — skipped.");
                    return null;

                case VRCPhysBoneColliderBase.ShapeType.Sphere:
                case VRCPhysBoneColliderBase.ShapeType.Capsule:
                {
                    var go = pbc.rootTransform != null ? pbc.rootTransform.gameObject : pbc.gameObject;
                    var dbc = go.AddComponent<DynamicBoneCollider>();

                    dbc.m_Bound = pbc.insideBounds
                        ? DynamicBoneColliderBase.Bound.Inside
                        : DynamicBoneColliderBase.Bound.Outside;

                    dbc.m_Center = pbc.position;
                    dbc.m_Radius = pbc.radius;

                    if (pbc.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule)
                    {
                        dbc.m_Height = pbc.height;
                        // Map rotation to direction axis
                        dbc.m_Direction = QuatToDirection(pbc.rotation);
                    }
                    else
                    {
                        dbc.m_Height = 0f;
                    }

                    return dbc;
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// Approximate the primary axis of a rotation as a DynamicBoneColliderBase.Direction enum.
        /// DB colliders align their capsule along the selected axis.
        /// </summary>
        private static DynamicBoneColliderBase.Direction QuatToDirection(Quaternion rotation)
        {
            var up = rotation * Vector3.up;
            float x = Mathf.Abs(up.x);
            float y = Mathf.Abs(up.y);
            float z = Mathf.Abs(up.z);

            if (x >= y && x >= z) return DynamicBoneColliderBase.Direction.X;
            if (z >= y && z >= x) return DynamicBoneColliderBase.Direction.Z;
            return DynamicBoneColliderBase.Direction.Y;
        }
    }
}
