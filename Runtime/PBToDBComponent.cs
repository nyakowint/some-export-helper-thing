using UnityEngine;

namespace Nyako.ExportHelper
{
    [AddComponentMenu("Beat Saber Export/PhysBones to DynamicBones")]
    public class PBToDBComponent : MonoBehaviour
    {
        [Tooltip("Folder inside the project to export (relative to Assets/). Leave empty to skip the export step.")]
        public string exportFolder = "Assets/MyAvatar";
    }
}
