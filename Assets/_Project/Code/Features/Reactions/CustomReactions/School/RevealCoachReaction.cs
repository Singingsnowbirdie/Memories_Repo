using Memories.Gameplay.Interaction;
using UnityEngine;
namespace Memories.Gameplay.Reactions
{
    public sealed class RevealCoachReaction : CustomReaction
    {
        [SerializeField] private ReactionInteractable giles;
        [SerializeField] private ReactionInteractable coach;
        [SerializeField] private SchoolGymEntranceReaction entrance;
        [SerializeField] private SchoolFirstAidProgressReaction supplies;
        public override void Execute()
        {
            giles.SetInteractionEnabled(false);
            coach.SetInteractionEnabled(true);
            coach.gameObject.SetActive(true);
            coach.GetComponent<ObjectStateTracker>()?.Capture();
            supplies.Begin();
        }
    }
}
