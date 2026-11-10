using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // The threshold's SceneTrigger graph explicitly starts this transition.
    public sealed class SchoolGymEntranceReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform corridorEntrance;
        [SerializeField] private GameObject corridor;
        [SerializeField] private GameObject[] garageOpening;
        [SerializeField] private Transform gym;
        [SerializeField] private Transform doorHinge;
        [SerializeField] private Transform lookTarget;
        [SerializeField, Min(.1f)] private float turnDuration = 1.2f;
        [SerializeField, Min(0)] private float holdDuration = 1.5f;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<Collider> hiddenColliders = new List<Collider>();
        private readonly List<Light> hiddenLights = new List<Light>();
        private PlayerMotor player;
        private PlayerMovementModel movement;
        private GameplayModeState state;
        private Func<UniTask> enterSchool;
        private CancellationTokenSource animationCancellation;
        private bool prepared, completed;
        private bool corridorOpened;
        public Transform CorridorEntrance => corridorEntrance;
        public bool IsRunning { get; private set; }
        public bool IsCompleted => completed;
        public string StateKey => "school-gym-entry";

        public void Initialize(PlayerMotor motor, PlayerMovementModel model, GameplayModeState mode, Func<UniTask> entry)
        {
            player = motor;
            movement = model;
            state = mode;
            enterSchool = entry;
        }

        // Called once by the explicit garage preload action, after binding saved objects.
        public void PrepareArrival()
        {
            if (prepared) return;
            completed = false;
            prepared = true;
            SetGarageOpeningActive(true);
            doorHinge.localRotation = Quaternion.Euler(0, 100, 0);
            // Component visibility is transient: do not save a temporarily hidden school as inactive objects.
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if (!KeepVisible(renderer.transform) && renderer.enabled)
                    { hiddenRenderers.Add(renderer); renderer.enabled = false; }
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    if (!KeepVisible(collider.transform) && collider.enabled)
                    { hiddenColliders.Add(collider); collider.enabled = false; }
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                    if (!KeepVisible(light.transform) && light.enabled)
                    { hiddenLights.Add(light); light.enabled = false; }
            }
            Physics.SyncTransforms();
        }

        private bool KeepVisible(Transform target) => target.IsChildOf(gym) || target.IsChildOf(transform);

        public void RestoreForDirectLoad()
        {
            completed = true;
            prepared = false;
            ShowSchoolAndSealDoor();
        }

        public override void Execute()
        {
            if (!prepared || completed || IsRunning || player == null || lookTarget == null || enterSchool == null) return;
            PlayAsync().Forget(exception =>
            {
                if (!(exception is OperationCanceledException)) Debug.LogException(exception, this);
            });
        }

        private async UniTask PlayAsync()
        {
            IsRunning = true;
            state.BlockControls(this);
            animationCancellation = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var token = animationCancellation.Token;
            try
            {
                var direction = lookTarget.position - player.CameraTransform.position;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float fromYaw = movement.Yaw, fromPitch = movement.Pitch;
                float angle = new Vector2(Mathf.DeltaAngle(fromYaw, yaw), pitch - fromPitch).magnitude;
                float duration = Mathf.Max(turnDuration, angle / 60f);
                for (float elapsed = 0; elapsed < duration;)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                    movement.SetInitialYaw(Mathf.LerpAngle(fromYaw, yaw, t));
                    movement.SetInitialPitch(Mathf.Lerp(fromPitch, pitch, t));
                    player.SetLook(movement.Yaw, movement.Pitch);
                    await UniTask.NextFrame(cancellationToken: token);
                }
                // The camera now faces away from the entrance. Commit scene ownership while controls stay locked.
                await enterSchool();
                token.ThrowIfCancellationRequested();
                ShowSchoolAndSealDoor();
                completed = true;
                prepared = false;
                await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: token);
            }
            finally
            {
                state.ReleaseControls(this);
                IsRunning = false;
                animationCancellation?.Dispose();
                animationCancellation = null;
            }
        }

        private void ShowSchoolAndSealDoor()
        {
            doorHinge.localRotation = Quaternion.identity;
            SetGarageOpeningActive(false);
            foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
            foreach (var collider in hiddenColliders) if (collider != null) collider.enabled = true;
            foreach (var light in hiddenLights) if (light != null) light.enabled = true;
            hiddenRenderers.Clear();
            hiddenColliders.Clear();
            hiddenLights.Clear();
            Physics.SyncTransforms();
        }

        private void SetGarageOpeningActive(bool active)
        {
            corridor.SetActive(active);
            foreach (var part in garageOpening)
                if (part != null) part.SetActive(active);
        }
        public void OpenCorridor()
        {
            corridorOpened = true;
            doorHinge.localRotation = Quaternion.Euler(0, 100, 0);
            Physics.SyncTransforms();
            GetComponent<ObjectStateTracker>()?.Capture();
        }

        public string CaptureState() => corridorOpened ? "corridor-open" : completed ? "complete" : "waiting";
        public void RestoreState(string value)
        {
            completed = value == "complete" || value == "corridor-open";
            corridorOpened = value == "corridor-open";
        }

        private void OnDisable()
        {
            animationCancellation?.Cancel();
            state?.ReleaseControls(this);
        }
    }
}
