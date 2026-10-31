using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class InteractionProbe
    {
        public const float Reach = 3f;
        private readonly Camera camera;
        private readonly Transform playerRoot;
        private RaycastHit[] hits = new RaycastHit[16];
        public InteractionProbe(Camera camera)
        {
            this.camera = camera;
            var controller = camera != null ? camera.GetComponentInParent<CharacterController>() : null;
            playerRoot = controller != null ? controller.transform : null;
        }

        public Interactable FindTarget()
        {
            if (camera == null || !camera.isActiveAndEnabled)
                return null;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            // Cast from the camera position so an obstacle inside the near clip is not skipped.
            ray.origin = camera.transform.position;
            if (!FindNearestHit(ray, out var hit))
                return null;
            var target = hit.collider.GetComponentInParent<Interactable>();
            bool available = target != null && target.CanInteract;
            return available ? target : null;
        }

        private bool FindNearestHit(Ray ray, out RaycastHit nearest)
        {
            int count;
            // A full buffer may omit the nearest obstacle: grow and repeat before filtering.
            while ((count = Physics.RaycastNonAlloc(ray, hits, Reach, Physics.DefaultRaycastLayers,
                       QueryTriggerInteraction.Ignore)) == hits.Length)
                System.Array.Resize(ref hits, hits.Length * 2);

            nearest = default;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (playerRoot != null && hit.collider.transform.IsChildOf(playerRoot)) continue;
                if (hit.distance >= distance) continue;
                nearest = hit;
                distance = hit.distance;
            }
            return nearest.collider != null;
        }
    }
}
