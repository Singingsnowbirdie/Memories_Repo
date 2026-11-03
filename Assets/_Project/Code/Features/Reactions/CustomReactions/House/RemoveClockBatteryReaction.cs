using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом кухонных часов после добавления их батарейки в инвентарь.
    /// Останавливает обе стрелки; следующий узел графа отключает взаимодействие с часами.
    /// Initialize однократно восстанавливает состояние по истории получения батарейки,
    /// включая потраченную батарейку. Update только вращает стрелки в противоположных направлениях.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionInteractable))]
    public sealed class RemoveClockBatteryReaction : CustomReaction
    {
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform hourHand;
        [SerializeField] private QuestItem battery;
        [SerializeField] private float minuteDegreesPerSecond = 110f;
        [SerializeField] private float hourDegreesPerSecond = -75f;
        public bool IsRunning { get; private set; } = true;

        public void Initialize(QuestInventory inventory)
        {
            IsRunning = battery != null && !inventory.WasCollected(battery.Id);
            GetComponent<ReactionInteractable>().SetInteractionEnabled(IsRunning);
        }

        public override void Execute() => IsRunning = false;

        private void Update()
        {
            if (!IsRunning) return;
            if (minuteHand != null)
                minuteHand.Rotate(Vector3.forward, minuteDegreesPerSecond * Time.deltaTime, Space.Self);
            if (hourHand != null)
                hourHand.Rotate(Vector3.forward, hourDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
