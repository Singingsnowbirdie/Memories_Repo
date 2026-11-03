using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом TV_BaseballMemory после просмотра первого воспоминания в доме.
    /// Также вызывается из Restore при восстановлении уже просмотренного воспоминания.
    /// Закрывает обе створки входной двери и включает их коллайдеры, блокируя проход.
    /// </summary>
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
            CloseLeaf(leftLeaf);
            CloseLeaf(rightLeaf);
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
