using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом самолётика после добавления его батарейки в инвентарь.
    /// Останавливает пропеллер; следующий узел графа отключает взаимодействие с самолётиком.
    /// Initialize однократно восстанавливает вращение и доступность по истории получения батарейки,
    /// включая уже потраченную батарейку. Update только анимирует пропеллер и не проверяет прогресс.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionInteractable))]
    public sealed class RemoveToyPlaneBatteryReaction : CustomReaction
    {
        [SerializeField] private Transform propeller;
        [SerializeField] private QuestItem battery;
        [SerializeField] private float degreesPerSecond = 720f;
        public bool IsSpinning { get; private set; } = true;

        public void Initialize(QuestInventory inventory)
        {
            IsSpinning = battery != null && !inventory.WasCollected(battery.Id);
            GetComponent<ReactionInteractable>().SetInteractionEnabled(IsSpinning);
        }

        public override void Execute() => IsSpinning = false;

        private void Update()
        {
            if (IsSpinning && propeller != null)
                propeller.Rotate(Vector3.forward, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
