using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace Nyako.ExportHelper.Editor
{
    public class PbToDBPass : Pass<PbToDBPass>
    {
        protected override void Execute(BuildContext ctx)
        {
            // Only run during a generic/manual bake — never during VRChat builds.
            // This prevents breaking VRChat uploads when the component is present.
            if (ctx.PlatformProvider.QualifiedName != WellKnownPlatforms.Generic)
                return;

            var component = ctx.AvatarRootObject.GetComponentInChildren<PBToDBComponent>(true);
            if (component == null)
                return;

            string exportFolder = component.exportFolder;

            // 1. Convert PhysBones → DynamicBones (also handles colliders internally)
            PbToDBConverter.Convert(ctx.AvatarRootObject);

            // 2. Strip all VRC-specific components (descriptor, remaining SDK components, etc.)
            //    This runs AFTER conversion so no VRC tooling is broken mid-pass.
            StripVrcComponents(ctx.AvatarRootObject);

            // 3. Schedule the export dialog to open after the bake finishes
            EditorApplication.delayCall += () => ExportPackageHelper.Show(exportFolder);
        }

        /// <summary>
        /// Destroys all components whose assembly originates from VRC SDK packages.
        /// Most other platforms dont support any VRC components.
        /// </summary>
        private static void StripVrcComponents(GameObject avatarRoot)
        {
            var allComponents = avatarRoot.GetComponentsInChildren<Component>(true);
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                // Skip Transform — it is not a VRC component and destroying it breaks things
                if (comp is Transform) continue;

                var asmName = comp.GetType().Assembly.GetName().Name;
                if (asmName.StartsWith("VRC.") ||
                    asmName.StartsWith("VRCSDK") ||
                    asmName.StartsWith("VRCSDKBase"))
                {
                    Object.DestroyImmediate(comp);
                }
            }
        }
    }
}
