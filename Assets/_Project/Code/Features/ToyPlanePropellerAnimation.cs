using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Memories.Gameplay.Reactions
{
    /// <summary>Управляет вращением и сохраняет состояние анимации.</summary>
    [MovedFrom(true, sourceNamespace: "Memories.Gameplay.Reactions", sourceAssembly: "Memories.Gameplay", sourceClassName: "StopToyPlanePropellerReaction")]
    [DisallowMultipleComponent]
    public sealed class ToyPlanePropellerAnimation : MonoBehaviour, IStoppableAnimation, IObjectStateParticipant
    {
        [SerializeField] private Transform propeller;
        [SerializeField] private float degreesPerSecond = 720f;
        public bool IsSpinning { get; private set; } = true;

        public string StateKey => "toy_plane.propeller";
        public string CaptureState() => IsSpinning ? "spinning" : "stopped";
        public void RestoreState(string state) => IsSpinning = state != "stopped";

        public void StopAnimation()
        {
            IsSpinning = false;
            GetComponent<ObjectStateTracker>()?.Capture();
        }

        private void Update()
        {
            if (IsSpinning && propeller != null)
                propeller.Rotate(Vector3.forward, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
