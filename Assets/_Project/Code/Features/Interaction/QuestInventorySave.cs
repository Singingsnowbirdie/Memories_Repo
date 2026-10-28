using System;
using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class QuestInventorySave : IDisposable
    {
        public const string DefaultKey = "Memories.Progress.Inventory.v1";
        [Serializable]
        private sealed class SavedInventory
        {
            public int version = 1;
            public List<string> ids = new List<string>();
        }

        private readonly QuestInventory inventory;
        private readonly string key;
        private readonly List<string> unresolved = new List<string>();

        public QuestInventorySave(QuestInventory inventory, IEnumerable<QuestItem> catalog, string key = DefaultKey)
        {
            this.inventory = inventory;
            this.key = key;
            var itemsById = new Dictionary<string, QuestItem>(StringComparer.Ordinal);
            foreach (var item in catalog)
                if (item != null && item.IsValid) itemsById[item.Id] = item;
            if (PlayerPrefs.HasKey(key))
            {
                try
                {
                    var saved = JsonUtility.FromJson<SavedInventory>(PlayerPrefs.GetString(key));
                    if (saved != null && saved.version == 1 && saved.ids != null)
                        foreach (string id in saved.ids)
                        {
                            if (string.IsNullOrWhiteSpace(id)) continue;
                            if (itemsById.TryGetValue(id, out var item)) inventory.TryAdd(item);
                            else if (!unresolved.Contains(id)) unresolved.Add(id);
                        }
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("Inventory save could not be read. Starting with an empty inventory.");
                }
            }
            inventory.Changed += Save;
        }

        private void Save()
        {
            var saved = new SavedInventory();
            foreach (var item in inventory.Items) saved.ids.Add(item.Id);
            foreach (string id in unresolved)
                if (!saved.ids.Contains(id)) saved.ids.Add(id);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
        }

        public static void Clear(string key = DefaultKey)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public void Dispose() => inventory.Changed -= Save;
    }
}
