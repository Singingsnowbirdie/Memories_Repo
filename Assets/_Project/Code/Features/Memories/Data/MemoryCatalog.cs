using System;
using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryCatalog : ScriptableObject
    {
        public const string ResourceName = "MemoriesComicCatalog";
        [Serializable]
        public sealed class Entry
        {
            public string key;
            public MemoryGraph graph;
        }
        public Entry[] entries = Array.Empty<Entry>();

        public static IEnumerable<Entry> LoadEntries()
        {
#if UNITY_EDITOR
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:MemoryGraph"))
            {
                var graph = UnityEditor.AssetDatabase.LoadAssetAtPath<MemoryGraph>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (graph != null) yield return new Entry { key = guid, graph = graph };
            }
#else
            var catalog = Resources.Load<MemoryCatalog>(ResourceName);
            if (catalog != null)
                foreach (var entry in catalog.entries) yield return entry;
#endif
        }
    }
}
