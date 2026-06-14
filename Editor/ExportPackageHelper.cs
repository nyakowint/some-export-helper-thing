using System;
using UnityEditor;
using UnityEngine;

namespace Nyako.ExportHelper.Editor
{
    internal static class ExportPackageHelper
    {
        internal static void Show(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("[BeatSaberExport] No export folder set — skipping export step.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
            string[] paths = Array.ConvertAll(guids, AssetDatabase.GUIDToAssetPath);

            if (paths.Length == 0)
            {
                Debug.LogWarning($"[BeatSaberExport] No assets found in '{folderPath}' — skipping export.");
                return;
            }

            AssetDatabase.ExportPackage(
                paths,
                "BeatSaber_Avatar_Export.unitypackage",
                ExportPackageOptions.Interactive |
                ExportPackageOptions.Recurse |
                ExportPackageOptions.IncludeDependencies
            );
        }
    }
}
