using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>Останавливает стрелки по явному действию графа и сохраняет состояние часов.</summary>
    [DisallowMultipleComponent]
    public sealed class StopClockHandsReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform hourHand;
        [SerializeField] private float minuteDegreesPerSecond = 110f;
        [SerializeField] private float hourDegreesPerSecond = -75f;
        public bool IsRunning { get; private set; } = true;

        public string StateKey => "clock.hands";
        public string CaptureState() => IsRunning ? "running" : "stopped";
        public void RestoreState(string state) => IsRunning = state != "stopped";

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
