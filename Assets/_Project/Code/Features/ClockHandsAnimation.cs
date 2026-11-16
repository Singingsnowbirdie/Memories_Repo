using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Memories.Gameplay.Reactions
{
    /// <summary>Управляет вращением и сохраняет состояние анимации.</summary>
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Reactions", sourceAssembly: "Memories.Gameplay", sourceClassName: "StopClockHandsReaction")]
    [DisallowMultipleComponent]
    public sealed class ClockHandsAnimation : MonoBehaviour, IStoppableAnimation, IObjectStateParticipant
    {
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform hourHand;
        [SerializeField] private float minuteDegreesPerSecond = 110f;
        [SerializeField] private float hourDegreesPerSecond = -75f;
        public bool IsRunning { get; private set; } = true;

        public string StateKey => "clock.hands";
        public string CaptureState() => IsRunning ? "running" : "stopped";
        public void RestoreState(string state) => IsRunning = state != "stopped";

        public void StopAnimation()
        {
            IsRunning = false;
            GetComponent<ObjectStateTracker>()?.Capture();
        }

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
