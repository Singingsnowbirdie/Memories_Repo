using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public abstract class CustomReaction : MonoBehaviour
    {
        public abstract void Execute();

        public static bool TryFind(Object owner, out CustomReaction reaction)
        {
            reaction = null;
            if (!(owner is Component component)) return false;
            foreach (var candidate in component.GetComponents<CustomReaction>())
            {
                if (!candidate.isActiveAndEnabled) continue;
                if (reaction != null) { reaction = null; return false; }
                reaction = candidate;
            }
            return reaction != null;
        }
    }
}
