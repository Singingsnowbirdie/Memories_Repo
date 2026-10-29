using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Open Courtyard Door Reaction")]
    public sealed class OpenCourtyardDoorReaction : CustomReaction
    {
        [SerializeField] private CourtyardDoor door;

        public override void Execute()
        {
            if (door == null)
            {
                Debug.LogError("Не назначена дверь для открытия.", this);
                return;
            }
            door.Open();
        }
    }
}
