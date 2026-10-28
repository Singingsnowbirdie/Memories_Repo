using System.Collections.Generic;
using System.Linq;
using Memories.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Memories.Editor
{
    public sealed class QuestItemCatalogBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var items = QuestItemCatalog.LoadItems().OrderBy(item => item.Id).ToArray();
            var ids = new HashSet<string>();
            foreach (var item in items)
                if (!item.IsValid || !ids.Add(item.Id))
                    throw new BuildFailedException("Quest items must have unique, nonempty IDs: " + AssetDatabase.GetAssetPath(item));

            const string folder = "Assets/_Project/Resources";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            string path = folder + "/" + QuestItemCatalog.ResourceName + ".asset";
            var catalog = AssetDatabase.LoadAssetAtPath<QuestItemCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<QuestItemCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            catalog.items = items;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
