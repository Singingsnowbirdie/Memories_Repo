using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryCollection
    {
        public const string DefaultKey = "Memories.Progress.Comics.v1";
        public sealed class FoundMemory
        {
            public string Id { get; }
            public MemoryGraph Graph { get; }
            public string Title => Graph.DisplayTitle;
            public Texture2D Cover => Graph.nodes.OfType<MemoryFrameNode>().FirstOrDefault()?.Image;
            public FoundMemory(string id, MemoryGraph graph) { Id = id; Graph = graph; }
        }
        [Serializable]
        private sealed class SavedMemory { public string id; public string graphKey; }
        [Serializable]
        private sealed class SaveData { public int version = 1; public List<SavedMemory> memories = new List<SavedMemory>(); }

        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<FoundMemory> memories = new List<FoundMemory>();
        private readonly SaveData saved = new SaveData();
        private readonly Dictionary<string, MemoryGraph> graphs = new Dictionary<string, MemoryGraph>(StringComparer.Ordinal);
        private readonly bool persistProgress;
        private readonly string saveKey;
        public IReadOnlyList<FoundMemory> Memories => memories.AsReadOnly();

        public MemoryCollection(IEnumerable<MemoryCatalog.Entry> catalog = null, bool persistProgress = false, string saveKey = DefaultKey)
        {
            this.persistProgress = persistProgress;
            this.saveKey = saveKey;
            if (catalog != null)
                foreach (var entry in catalog)
                    if (entry?.graph != null && !string.IsNullOrWhiteSpace(entry.key)) graphs[entry.key] = entry.graph;
            if (!persistProgress || !PlayerPrefs.HasKey(saveKey)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey));
                if (data?.version != 1 || data.memories == null) return;
                foreach (var entry in data.memories)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !collected.Add(entry.id)) continue;
                    saved.memories.Add(entry);
                    if (!string.IsNullOrEmpty(entry.graphKey) && graphs.TryGetValue(entry.graphKey, out var graph))
                        memories.Add(new FoundMemory(entry.id, graph));
                }
            }
            catch (ArgumentException) { Debug.LogWarning("Не удалось загрузить копилку воспоминаний."); }
        }

        public bool Contains(string id) => collected.Contains(id);
        public bool Collect(string id, MemoryGraph graph = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Memory ID is required.", nameof(id));
            bool added = collected.Add(id);
            var entry = saved.memories.Find(item => item.id == id);
            if (entry == null) { entry = new SavedMemory { id = id }; saved.memories.Add(entry); }
            if (graph != null)
            {
                entry.graphKey = graphs.FirstOrDefault(pair => pair.Value == graph).Key;
                if (!memories.Any(item => item.Id == id)) memories.Add(new FoundMemory(id, graph));
            }
            if (persistProgress)
            {
                PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(saved));
                PlayerPrefs.Save();
            }
            return added;
        }

        public static void Clear(string saveKey = DefaultKey)
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
        }
    }
}
