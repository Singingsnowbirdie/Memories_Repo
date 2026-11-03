using System.Collections;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class FocusCameraReaction : CustomReaction
    {
        [SerializeField] private Renderer target;
        [SerializeField, Min(.01f)] private float travelDuration = .8f;
        [SerializeField, Min(0)] private float holdDuration = 2f;
        private PlayerMotor player;
        private GameplayModeState state;
        private Transform cameraTransform;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool previousCameraControl, running;
        public bool IsApproaching { get; private set; }

        public void Initialize(PlayerMotor player, GameplayModeState state)
        { this.player = player; this.state = state; }

        public override void Execute()
        {
            if (running || target == null || player == null || player.CameraTransform == null) return;
            StartCoroutine(Focus());
        }

        private IEnumerator Focus()
        {
            running = true;
            IsApproaching = true;
            cameraTransform = player.CameraTransform;
            originalPosition = cameraTransform.localPosition;
            originalRotation = cameraTransform.localRotation;
            previousCameraControl = player.CameraPoseControlled;
            player.CameraPoseControlled = true;
            state?.BlockControls(this);
            try
            {
                var bounds = target.bounds;
                if (!target.gameObject.activeInHierarchy)
                {
                    var localBounds = target.localBounds;
                    var matrix = target.localToWorldMatrix;
                    var x = matrix.MultiplyVector(new Vector3(localBounds.extents.x, 0, 0));
                    var y = matrix.MultiplyVector(new Vector3(0, localBounds.extents.y, 0));
                    var z = matrix.MultiplyVector(new Vector3(0, 0, localBounds.extents.z));
                    var extents = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                        Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                        Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                    bounds = new Bounds(matrix.MultiplyPoint3x4(localBounds.center), extents * 2);
                }
                var center = bounds.center;
                var offset = cameraTransform.position - center;
                if (offset.sqrMagnitude > .001f)
                {
                    float distance = Mathf.Min(offset.magnitude, Mathf.Max(.75f, bounds.size.magnitude));
                    var position = center + offset.normalized * distance;
                    var rotation = Quaternion.LookRotation(center - position, Vector3.up);
                    yield return Move(cameraTransform.parent.InverseTransformPoint(position),
                        Quaternion.Inverse(cameraTransform.parent.rotation) * rotation);
                    IsApproaching = false;
                    yield return new WaitForSeconds(holdDuration);
                    yield return Move(originalPosition, originalRotation);
                }
            }
            finally { Restore(); }
        }

        private IEnumerator Move(Vector3 position, Quaternion rotation)
        {
            var fromPosition = cameraTransform.localPosition;
            var fromRotation = cameraTransform.localRotation;
            float elapsed = 0;
            while (elapsed < travelDuration && cameraTransform != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.01f, travelDuration));
                cameraTransform.SetLocalPositionAndRotation(Vector3.Lerp(fromPosition, position, t),
                    Quaternion.Slerp(fromRotation, rotation, t));
                yield return null;
            }
        }

        private void Restore()
        {
            IsApproaching = false;
            if (!running) return;
            if (cameraTransform != null) cameraTransform.SetLocalPositionAndRotation(originalPosition, originalRotation);
            if (player != null) player.CameraPoseControlled = previousCameraControl;
            state?.ReleaseControls(this);
            cameraTransform = null;
            running = false;
        }

        private void OnDisable() { StopAllCoroutines(); Restore(); }
    }
}
