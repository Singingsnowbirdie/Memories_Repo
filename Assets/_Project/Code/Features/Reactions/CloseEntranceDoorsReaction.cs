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
        [SerializeField] private Transform[] additionalLeaves = new Transform[0];
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
            CloseLeaf(leftLeaf);
            CloseLeaf(rightLeaf);
            foreach (var leaf in additionalLeaves) CloseLeaf(leaf);
            Physics.SyncTransforms();
        }

        private static void CloseLeaf(Transform leaf)
        {
            if (leaf == null) return;
            leaf.localRotation = Quaternion.identity;
            foreach (var collider in leaf.GetComponentsInChildren<Collider>(true))
            {
                collider.isTrigger = false;
                collider.enabled = true;
            }
        }
    }
}
