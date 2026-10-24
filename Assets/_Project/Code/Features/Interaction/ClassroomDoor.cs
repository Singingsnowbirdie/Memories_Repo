using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class ClassroomDoor : Interactable
    {
        [SerializeField] private Transform hinge;
        [SerializeField, Min(0.1f)] private float duration = 1f;
        private Quaternion closedRotation;
        private float progress;
        private bool opening;
        public bool IsOpen => progress >= 1f;
        public override string Prompt => "Открыть дверь класса";
        public override bool CanInteract => base.CanInteract && hinge != null && !opening && !IsOpen;

        private void Awake()
        {
            if (hinge != null) closedRotation = hinge.localRotation;
        }

        public override void Interact()
        {
            if (CanInteract) opening = true;
        }

        private void Update()
        {
            if (!opening) return;
            progress = Mathf.Min(1f, progress + Time.deltaTime / duration);
            hinge.localRotation = closedRotation * Quaternion.Euler(0, 100f * Mathf.SmoothStep(0, 1, progress), 0);
            if (IsOpen) opening = false;
        }
    }
}