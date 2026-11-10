using UnityEngine;
namespace Memories.Gameplay.Reactions
{
    public sealed class OpenGymCorridorReaction : CustomReaction
    {
        [SerializeField] private SchoolGymEntranceReaction entrance;
        public override void Execute() => entrance.OpenCorridor();
    }
}
