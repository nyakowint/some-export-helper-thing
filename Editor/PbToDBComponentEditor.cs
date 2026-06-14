using nadena.dev.ndmf;
using nadena.dev.ndmf.platform;
using UnityEditor;
using UnityEngine;

namespace Nyako.ExportHelper.Editor
{
    [CustomEditor(typeof(PBToDBComponent))]
    public class PbToDBComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var comp = (PBToDBComponent)target;

            EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // --- Export Folder ---
            EditorGUILayout.BeginHorizontal();
            comp.exportFolder = EditorGUILayout.TextField("Export Folder", comp.exportFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel(
                    "Select Export Folder",
                    string.IsNullOrEmpty(comp.exportFolder) ? "Assets" : comp.exportFolder,
                    "");

                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                        comp.exportFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                    else
                        Debug.LogWarning("[LN Export Helper] Selected folder must be inside the project Assets folder.");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (!string.IsNullOrEmpty(comp.exportFolder))
                EditorGUILayout.HelpBox($"Will export contents of: {comp.exportFolder}", MessageType.Info);
            else
                EditorGUILayout.HelpBox("No export folder set. The export step will be skipped.", MessageType.Warning);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(4);

            // --- Export Button ---
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);

            if (GUILayout.Button("Bake avatar", GUILayout.Height(40)))
            {
                GUI.backgroundColor = prevColor;
                RunExport(comp);
                return;
            }

            GUI.backgroundColor = prevColor;

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "Triggers an NDMF manual bake on the avatar:\n" +
                "  1. Runs all NDMF passes (Modular Avatar, etc.)\n" +
                "  2. Converts PhysBones → DynamicBones\n" +
                "  3. Strips all VRChat-specific components\n" +
                "  4. Opens the Export Package dialog",
                MessageType.None);

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private static void RunExport(PBToDBComponent comp)
        {
            // Walk up to the avatar root (component may be on a child, but usually on the root)
            var avatarRoot = comp.gameObject;
            while (avatarRoot.transform.parent != null)
                avatarRoot = avatarRoot.transform.parent.gameObject;

            Debug.Log($"[LN Export Helper] Starting export bake on '{avatarRoot.name}'...");

            // Run NDMF manual bake on the Generic platform.
            // PBToDBPass detects this platform and runs the full pipeline.
            AvatarProcessor.ManualProcessAvatar(avatarRoot, GenericPlatform.Instance);
        }
    }
}
