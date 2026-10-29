using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    // Restores scene presentation from saved quest state without replaying designer reactions.
    public sealed class WomanTelevisionState : MonoBehaviour
    {
        [SerializeField] private CustomVariableBank variablesBank;
        private CustomVariableStore variables;
        public bool IsTelevisionOn => variables != null && variablesBank != null &&
            variablesBank.Find("television_on") != null && variables.Get(variablesBank, "television_on");

        public void Initialize(CustomVariableStore store, ReactionObjectBinding[] bindings)
        {
            variables = store;
            bool on = IsTelevisionOn;
            Set(bindings, "TV static", !on);
            Set(bindings, "TV baseball", on);
            Set(bindings, "Hand remote", on);
        }

        private static void Set(ReactionObjectBinding[] bindings, string key, bool active)
        {
            if (ReactionObjectBinding.TryResolve(bindings, key, out var target)) target.SetActive(active);
        }
    }
}
