using UnityEngine;

namespace Nyako.ExportHelper
{
    [AddComponentMenu("Nyako/LN Export Helper")]
    public class ExportHelperComponent : MonoBehaviour
    {
        [Tooltip("Folder inside the project to export (relative to Assets/). Leave empty to skip the export step.")]
        public string exportFolder = "Assets/LNE_Export";
    }
}
