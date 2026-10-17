using System;
using System.Collections.Generic;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryCollection
    {
        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);
        public bool Contains(string id) => collected.Contains(id);
        public bool Collect(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Memory ID is required.", nameof(id));
            return collected.Add(id);
        }
    }
}
