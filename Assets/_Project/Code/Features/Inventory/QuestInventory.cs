using System;
using System.Collections.Generic;

namespace Memories.Gameplay.Interaction
{
    public sealed class QuestInventory
    {
        private readonly List<QuestItem> items = new List<QuestItem>();
        private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> consumed = new HashSet<string>(StringComparer.Ordinal);
        public IEnumerable<string> ConsumedIds => consumed;
        public bool IsConsumed(string id) => id != null && consumed.Contains(id);
        public bool WasCollected(string id) => Contains(id) || IsConsumed(id);
        public void RestoreConsumed(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            consumed.Add(id);
            ids.Remove(id);
            items.RemoveAll(item => item.Id == id);
        }
        public bool TryConsume(QuestItem item)
        {
            if (item == null || !ids.Remove(item.Id)) return false;
            items.RemoveAll(value => value.Id == item.Id);
            consumed.Add(item.Id);
            Changed?.Invoke();
            return true;
        }
        public IReadOnlyList<QuestItem> Items { get; }
        public event Action Changed;
        public QuestInventory() => Items = items.AsReadOnly();
        public bool Contains(string id) => id != null && ids.Contains(id);

        public bool TryAdd(QuestItem item)
        {
            if (item == null || !item.IsValid || consumed.Contains(item.Id) || !ids.Add(item.Id)) return false;
            items.Add(item);
            Changed?.Invoke();
            return true;
        }
    }
}
