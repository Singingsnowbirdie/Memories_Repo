using System;
using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [Serializable]
    public sealed class CustomReactionBinding
    {
        [SerializeField] private string key;
        [SerializeField] private CustomReaction reaction;
        public CustomReactionBinding(string key, CustomReaction reaction) { this.key = key; this.reaction = reaction; }

        public static bool TryResolve(IReadOnlyList<CustomReactionBinding> bindings, string key, out CustomReaction reaction)
        {
            reaction = null;
            bool found = false;
            if (bindings == null) return false;
            foreach (var binding in bindings)
            {
                if (binding == null || (binding.key ?? "") != (key ?? "")) continue;
                if (found) { reaction = null; return false; }
                found = true;
                reaction = binding.reaction;
            }
            return reaction != null && reaction.isActiveAndEnabled;
        }
    }

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
