using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Interaction
{
    public sealed class CourtyardDoor : Interactable
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Renderer view;
        [SerializeField] private Collider closedDoor;
        [SerializeField] private string destination = "Assets/_Project/Locations/CourtyardIsland/CourtyardIsland.unity";
        [SerializeField] private string arrivalMarker = "Вход из дома";
        [SerializeField, Min(0f)] private float arrivalForwardOffset;
        [SerializeField, Tooltip("When set, the door opens automatically after this memory is collected and cannot be interacted with.")]
        private string requiredMemoryId;
        private MemoryCollection memories;
        private Func<string, UniTask<Scene>> load;
        private Func<Scene, Vector3, Quaternion, float, UniTask> enter;
        private PlayerMotor player;
        private Camera playerCamera, portalCamera;
        private RenderTexture texture;
        private Material material;
        private Transform arrival;
        private Scene courtyard;
        private bool opening, opened, crossing;
        private float previousDistance;
        public bool IsOpen => opened;
        public override string Prompt => string.IsNullOrWhiteSpace(requiredMemoryId) ? "Открыть дверь" : string.Empty;
        public override bool CanInteract => string.IsNullOrWhiteSpace(requiredMemoryId) && base.CanInteract && !opening && !opened && load != null;

        public void Initialize(PlayerMotor motor, Func<string, UniTask<Scene>> loader,
            Func<Scene, Vector3, Quaternion, float, UniTask> traveller, MemoryCollection collection = null)
        {
            if (memories != null) memories.Collected -= OnMemoryCollected;
            player = motor; playerCamera = motor.GetComponentInChildren<Camera>(); load = loader; enter = traveller;
            memories = collection;
            if (memories == null || string.IsNullOrWhiteSpace(requiredMemoryId)) return;
            memories.Collected += OnMemoryCollected;
            if (memories.Contains(requiredMemoryId)) OnMemoryCollected(requiredMemoryId);
        }

        private void OnMemoryCollected(string id)
        {
            if (id == requiredMemoryId && !opening && !opened && load != null)
            {
                opening = true;
                OpenAsync().Forget();
            }
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            opening = true;
            OpenAsync().Forget();
        }

        private async UniTask OpenAsync()
        {
            try
            {
                // A restored memory can be bound while the session is still finishing its scene load.
                if (!string.IsNullOrWhiteSpace(requiredMemoryId)) await UniTask.NextFrame();
                if (this == null) return;
                courtyard = await load(destination);
                if (this == null) return;
                foreach (var root in courtyard.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == arrivalMarker) arrival = t;
                if (arrival == null) throw new InvalidOperationException("Destination entrance marker is missing: " + arrivalMarker);
                var go = new GameObject("Вид двора через дверь");
                go.transform.SetParent(transform, false);
                portalCamera = go.AddComponent<Camera>();
                portalCamera.CopyFrom(playerCamera);
                portalCamera.enabled = false;
                portalCamera.cullingMask = 1 << 30;
                portalCamera.clearFlags = CameraClearFlags.SolidColor;
                portalCamera.backgroundColor = Color.black;
                var destinationSky = arrival.root.GetComponentInChildren<Skybox>(true);
                if (destinationSky != null && destinationSky.material != null)
                {
                    go.AddComponent<Skybox>().material = destinationSky.material;
                    portalCamera.clearFlags = CameraClearFlags.Skybox;
                }
                texture = new RenderTexture(Mathf.Max(640, Screen.width), Mathf.Max(360, Screen.height), 24);
                texture.Create();
                portalCamera.targetTexture = texture;
                material = new Material(Shader.Find("Memories/PortalView"));
                material.SetTexture("_MainTex", texture);
                view.sharedMaterial = material;
                view.enabled = true;
                UpdateView();
                // The destination is rendered before the leaf starts moving.
                Quaternion start = hinge.localRotation;
                for (float elapsed = 0; elapsed < .85f; elapsed += Time.deltaTime)
                {
                    if (this == null) return;
                    hinge.localRotation = start * Quaternion.Euler(0, -100 * Mathf.SmoothStep(0, 1, elapsed / .85f), 0);
                    await UniTask.Yield();
                }
                hinge.localRotation = start * Quaternion.Euler(0, -100, 0);
                closedDoor.enabled = false;
                previousDistance = transform.InverseTransformPoint(player.transform.position).z;
                opened = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (this != null) opening = false;
            }
        }

        private void LateUpdate()
        {
            if (portalCamera == null || playerCamera == null || arrival == null) return;
            UpdateView();
            if (!opened || crossing) return;
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            if (Mathf.Abs(local.x) < .65f && local.z >= -.42f && previousDistance < -.42f)
            {
                crossing = true;
                // Keep the visual portal aligned, but place the capsule clear of the destination door.
                Vector3 position = arrival.TransformPoint(local + Vector3.forward * arrivalForwardOffset);
                Quaternion rotation = arrival.rotation * Quaternion.Inverse(transform.rotation) * player.transform.rotation;
                float pitch = Mathf.DeltaAngle(0, playerCamera.transform.localEulerAngles.x);
                enter(courtyard, position, rotation, pitch).Forget();
            }
            previousDistance = local.z;
        }

        private void UpdateView()
        {
            portalCamera.transform.SetPositionAndRotation(
                arrival.TransformPoint(transform.InverseTransformPoint(playerCamera.transform.position)),
                arrival.rotation * Quaternion.Inverse(transform.rotation) * playerCamera.transform.rotation);
            portalCamera.fieldOfView = playerCamera.fieldOfView;
            portalCamera.aspect = playerCamera.aspect;
            portalCamera.projectionMatrix = playerCamera.projectionMatrix;
            // Clip the destination wall/closed side door between the remote camera and the entrance.
            Vector3 clipPoint = portalCamera.worldToCameraMatrix.MultiplyPoint(arrival.position);
            Vector3 clipNormal = portalCamera.worldToCameraMatrix.MultiplyVector(arrival.forward).normalized;
            var clipPlane = new Vector4(clipNormal.x, clipNormal.y, clipNormal.z, -Vector3.Dot(clipPoint, clipNormal));
            portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipPlane);
            var previous = SceneManager.GetActiveScene();
            try
            {
                SceneManager.SetActiveScene(courtyard);
                portalCamera.Render();
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private void OnDestroy()
        {
            if (memories != null) memories.Collected -= OnMemoryCollected;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (material != null) Destroy(material);
        }
    }
}
