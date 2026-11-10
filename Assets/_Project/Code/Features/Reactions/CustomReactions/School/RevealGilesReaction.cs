using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Invoked explicitly by the first school memory's start action.
    public sealed class RevealGilesReaction : CustomReaction
    {
        [SerializeField] private ReactionInteractable giles;
        [SerializeField] private ReactionInteractable coach;

        public override void Execute()
        {
            if (coach != null && coach.InteractionEnabled) return;
            giles.SetInteractionEnabled(true);
            giles.gameObject.SetActive(true);
            giles.GetComponent<ObjectStateTracker>()?.Capture();
        }
    }
}
