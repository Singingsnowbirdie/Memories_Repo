using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class InteractionProbe
    {
        public const float Reach = 3f;
        private readonly Camera camera;
        public InteractionProbe(Camera camera) => this.camera = camera;

        public Interactable FindTarget()
        {
            if (camera == null || !camera.isActiveAndEnabled) return null;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            // Cast from the camera position so an obstacle inside the near clip is not skipped.
            ray.origin = camera.transform.position;
            if (!Physics.Raycast(ray, out var hit, Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return null;
            var target = hit.collider.GetComponentInParent<Interactable>();
            return target != null && target.CanInteract ? target : null;
        }
    }
}
