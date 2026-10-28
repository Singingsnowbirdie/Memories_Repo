using System;
using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [Serializable]
    public sealed class ReactionObjectBinding
    {
        [SerializeField] private string key;
        [SerializeField] private GameObject target;
        public ReactionObjectBinding(string key, GameObject target) { this.key = key; this.target = target; }

        public static bool TryResolve(IReadOnlyList<ReactionObjectBinding> bindings, string key, out GameObject target)
        {
            target = null;
            bool found = false;
            if (bindings == null || string.IsNullOrWhiteSpace(key)) return false;
            foreach (var binding in bindings)
            {
                if (binding == null || binding.key != key) continue;
                if (found) { target = null; return false; }
                found = true;
                target = binding.target;
            }
            return target != null;
        }
    }
}
