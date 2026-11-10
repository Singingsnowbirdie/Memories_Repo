using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class ReturnSchoolSuppliesReaction : CustomReaction
    {
        [SerializeField] private SchoolFirstAidProgressReaction progress;
        public override void Execute() => progress.ReturnToCoach();
    }
}
