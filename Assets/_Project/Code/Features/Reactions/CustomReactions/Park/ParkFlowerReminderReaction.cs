using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Invoked explicitly by the entrance graph.
    public sealed class ParkFlowerReminderReaction : CustomReaction
    {
        public const string HintId = "park.buy-flowers";
        [SerializeField] private ReactionInteractable reminder;

        public override void Execute()
        {
            reminder.Interact();
        }
    }
}
