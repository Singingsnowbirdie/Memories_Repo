using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>Останавливает пропеллер по явному действию графа и сохраняет состояние вращения.</summary>
    [DisallowMultipleComponent]
    public sealed class StopToyPlanePropellerReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform propeller;
        [SerializeField] private float degreesPerSecond = 720f;
        public bool IsSpinning { get; private set; } = true;

        public string StateKey => "toy_plane.propeller";
        public string CaptureState() => IsSpinning ? "spinning" : "stopped";
        public void RestoreState(string state) => IsSpinning = state != "stopped";

        public override void Execute() => IsSpinning = false;

        private void Update()
        {
            if (IsSpinning && propeller != null)
                propeller.Rotate(Vector3.forward, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
