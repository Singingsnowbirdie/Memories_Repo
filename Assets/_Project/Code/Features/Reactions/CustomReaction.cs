using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public abstract class CustomReaction : MonoBehaviour
    {
        public abstract void Execute();
        // При замене сцены воспроизведение остаётся у постоянного презентера воспоминаний.
        public virtual bool TransfersMemoryOwnership => false;

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
