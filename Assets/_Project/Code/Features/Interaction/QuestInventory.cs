using System;
using System.Collections.Generic;

namespace Memories.Gameplay.Interaction
{
    public sealed class QuestInventory
    {
        private readonly List<QuestItem> items = new List<QuestItem>();
        private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        public IReadOnlyList<QuestItem> Items { get; }
        public event Action Changed;
        public QuestInventory() => Items = items.AsReadOnly();
        public bool Contains(string id) => id != null && ids.Contains(id);

        public bool TryAdd(QuestItem item)
        {
            if (item == null || !item.IsValid || !ids.Add(item.Id)) return false;
            items.Add(item);
            Changed?.Invoke();
            return true;
        }
    }
}
