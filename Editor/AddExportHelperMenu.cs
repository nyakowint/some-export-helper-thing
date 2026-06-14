using UnityEditor;
using UnityEngine;

#if EXPORTHELPER_VRCSDK_PRESENT
using VRC.SDK3.Avatars.Components;
#endif

namespace Nyako.ExportHelper.Editor
{
    internal static class AddExportHelperMenu
    {
        private const string ToolsMenuPath = "Tools/Nyako/Add Export Helper";
        private const string GameObjectMenuPath = "GameObject/Nyako/Add Export Helper";

        [MenuItem(ToolsMenuPath)]
        private static void AddFromToolsMenu()
        {
            var root = FindAvatarRoot(null);
            if (root != null)
                AddComponentToAvatar(root);
            else
                AddExportHelperWindow.Open(null);
        }

        [MenuItem(GameObjectMenuPath, false, 20)]
        private static void AddFromGameObjectMenu(MenuCommand command)
        {
            var root = FindAvatarRoot(command.context as GameObject);
            if (root != null)
                AddComponentToAvatar(root);
            else
                AddExportHelperWindow.Open(command.context as GameObject);
        }

        [MenuItem(GameObjectMenuPath, true)]
        private static bool ValidateGameObjectMenu() => true;

        private static GameObject FindAvatarRoot(GameObject hint)
        {
            var candidate = hint != null ? hint : Selection.activeGameObject;
            if (candidate == null) return null;

#if EXPORTHELPER_VRCSDK_PRESENT
            var desc = candidate.GetComponent<VRCAvatarDescriptor>()
                       ?? candidate.GetComponentInParent<VRCAvatarDescriptor>();
            return desc != null ? desc.gameObject : null;
#else
            return candidate;
#endif
        }

        internal static void AddComponentToAvatar(GameObject avatarRoot)
        {
            var existing = avatarRoot.GetComponentInChildren<ExportHelperComponent>(true);
            if (existing != null)
            {
                EditorGUIUtility.PingObject(existing.gameObject);
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[LN Export Helper] ExportHelperComponent already exists — selecting it.");
                return;
            }

            var go = new GameObject("LN Export Helper");
            Undo.RegisterCreatedObjectUndo(go, "Add Export Helper");
            go.transform.SetParent(avatarRoot.transform, false);
            Undo.AddComponent<ExportHelperComponent>(go);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log($"[LN Export Helper] Added ExportHelperComponent under '{avatarRoot.name}'.");
        }
    }

    internal class AddExportHelperWindow : EditorWindow
    {
        private GameObject _avatarRoot;

        internal static void Open(GameObject preselect)
        {
            var window = GetWindow<AddExportHelperWindow>(true, "Add Export Helper");
            window.minSize = new Vector2(320, 110);
            window.maxSize = new Vector2(600, 110);
            window._avatarRoot = preselect;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("No avatar root found in selection.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(4);
            _avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar Root", _avatarRoot, typeof(GameObject), true);
            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledGroupScope(_avatarRoot == null))
            {
                if (GUILayout.Button("Add Export Helper", GUILayout.Height(28)))
                {
                    AddExportHelperMenu.AddComponentToAvatar(_avatarRoot);
                    Close();
                }
            }
        }
    }
}
