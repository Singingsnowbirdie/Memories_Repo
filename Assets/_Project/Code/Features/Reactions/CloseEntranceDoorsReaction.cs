using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Close Entrance Doors Reaction")]
    public sealed class CloseEntranceDoorsReaction : CustomReaction
    {
        [SerializeField] private Transform leftLeaf;
        [SerializeField] private Transform rightLeaf;
        [SerializeField] private string completedMemoryId;

        public void Restore(MemoryCollection memories)
        {
            if (!string.IsNullOrWhiteSpace(completedMemoryId) && memories.Contains(completedMemoryId)) Execute();
        }

        public override void Execute()
        {
            if (leftLeaf == null || rightLeaf == null)
            {
                Debug.LogError("Назначьте обе створки входной двери.", this);
                return;
            }
            leftLeaf.localRotation = Quaternion.identity;
            rightLeaf.localRotation = Quaternion.identity;
            foreach (var leaf in new[] { leftLeaf, rightLeaf })
                foreach (var collider in leaf.GetComponentsInChildren<Collider>(true))
                {
                    collider.isTrigger = false;
                    collider.enabled = true;
                }
            Physics.SyncTransforms();
        }
    }
}
