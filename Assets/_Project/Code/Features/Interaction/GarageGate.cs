using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    // The entire panel follows a tilt-up trajectory into the ceiling rails.
    // test

    public sealed class GarageGate : Interactable
    {
        [SerializeField] private Transform panel;
        [SerializeField, Min(0.1f)] private float duration = 3.5f;
        [SerializeField] private Vector3 raisedPosition = new Vector3(0, 3.35f, 1.65f);
        private Vector3 closedPosition;
        private Quaternion closedRotation;
        private float progress;
        private bool raising;
        public bool IsOpen => progress >= 1;
        public override string Prompt => "Поднять гаражные ворота";
        public override bool CanInteract => base.CanInteract && panel != null && !raising && !IsOpen;
        private void Awake()
        {
            if (panel == null) return;
            closedPosition = panel.localPosition;
            closedRotation = panel.localRotation;
        }
        public override void Interact()
        {
            if (CanInteract) raising = true;
        }
        private void Update()
        {
            if (!raising) return;
            progress = Mathf.Min(1, progress + Time.deltaTime / duration);
            float t = Mathf.SmoothStep(0, 1, progress);
            panel.localPosition = Vector3.Lerp(closedPosition, raisedPosition, t);
            panel.localRotation = closedRotation * Quaternion.Euler(90 * t, 0, 0);
            if (IsOpen) raising = false;
        }
    }
}
