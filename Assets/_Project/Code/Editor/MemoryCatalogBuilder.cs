using System.Linq;
using Memories.Gameplay.Memories;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Memories.Editor
{
    public sealed class MemoryCatalogBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            const string path = "Assets/_Project/Resources/" + MemoryCatalog.ResourceName + ".asset";
            var catalog = AssetDatabase.LoadAssetAtPath<MemoryCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MemoryCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            // Include every graph, including ones referenced only by scene triggers.
            catalog.entries = MemoryCatalog.LoadEntries().OrderBy(entry => entry.key).ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
