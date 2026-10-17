using UnityEngine;

namespace Memories.Gameplay.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour, IPlayerView
    {
        private CharacterController controller;
        private Transform cameraTransform;
        public float InitialYaw { get; private set; }
        public bool IsGrounded => controller.isGrounded;

        public void Initialize(PlayerMovementSettings settings, Transform viewCamera)
        {
            settings.Validate();
            controller = GetComponent<CharacterController>();
            controller.height = settings.Height;
            controller.radius = settings.Radius;
            controller.center = Vector3.up * (settings.Height / 2f);
            controller.stepOffset = settings.StepOffset;
            controller.slopeLimit = settings.SlopeLimit;
            controller.skinWidth = settings.Radius * 0.1f;
            controller.minMoveDistance = 0;
            InitialYaw = transform.eulerAngles.y;
            cameraTransform = viewCamera;
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition = Vector3.up * settings.EyeHeight;
            cameraTransform.localRotation = Quaternion.identity;
        }

        public void SetLook(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        public CollisionFlags Move(Vector3 displacement) => controller.Move(displacement);
    }
}
