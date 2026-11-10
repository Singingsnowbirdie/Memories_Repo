using Memories.Gameplay.Interaction;
using UnityEngine;
namespace Memories.Gameplay.Reactions
{
    public sealed class StopFirstAidReminderReaction : CustomReaction
    {
        [SerializeField] private ReactionInteractable coach;
        public override void Execute() => coach.StopHint("school.first-aid-kit");
    }
}
