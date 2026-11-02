using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Close Stair Door Reaction")]
    public sealed class CloseStairDoorReaction : CustomReaction
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private string completedMemoryId;

        public void Restore(MemoryCollection memories)
        {
            if (!string.IsNullOrWhiteSpace(completedMemoryId) && memories.Contains(completedMemoryId)) Execute();
        }

        public override void Execute()
        {
            if (hinge == null) { Debug.LogError("Назначьте петли двери у лестницы.", this); return; }
            hinge.localRotation = Quaternion.identity;
            foreach (var collider in hinge.GetComponentsInChildren<Collider>(true))
            {
                collider.isTrigger = false;
                collider.enabled = true;
            }
            Physics.SyncTransforms();
        }
    }
}
