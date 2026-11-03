using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class QuestItemCatalog : ScriptableObject
    {
        public const string ResourceName = "MemoriesQuestItemCatalog";
        public QuestItem[] items = new QuestItem[0];

        public static IEnumerable<QuestItem> LoadItems()
        {
#if UNITY_EDITOR
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:QuestItem"))
                yield return UnityEditor.AssetDatabase.LoadAssetAtPath<QuestItem>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
#else
            var catalog = Resources.Load<QuestItemCatalog>(ResourceName);
            if (catalog != null)
                foreach (var item in catalog.items) yield return item;
#endif
        }
    }
}
