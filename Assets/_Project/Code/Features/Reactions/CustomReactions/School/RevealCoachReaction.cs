using Memories.Gameplay.Interaction;
using UnityEngine;
namespace Memories.Gameplay.Reactions
{
    public sealed class RevealCoachReaction : CustomReaction
    {
        [SerializeField] private ReactionInteractable giles;
        [SerializeField] private ReactionInteractable coach;
        public override void Execute()
        {
            giles.SetInteractionEnabled(false);
            coach.SetInteractionEnabled(true);
            coach.gameObject.SetActive(true);
            coach.GetComponent<ObjectStateTracker>()?.Capture();
        }
    }
}
