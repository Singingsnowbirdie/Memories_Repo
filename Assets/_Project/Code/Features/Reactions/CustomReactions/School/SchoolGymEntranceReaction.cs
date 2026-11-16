using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Этот переход явно запускает граф SceneTrigger на пороге.
    public sealed class SchoolGymEntranceReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform corridorEntrance;
        [SerializeField] private GameObject corridor;
        [SerializeField] private GameObject[] garageOpening;
        [SerializeField] private Transform gym;
        [SerializeField] private GameObject closedDoor;
        [SerializeField] private GameObject openDoor;
        [SerializeField, Min(0)] private float holdDuration = 1.5f;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private readonly List<Collider> hiddenColliders = new List<Collider>();
        private readonly List<Light> hiddenLights = new List<Light>();
        [SerializeField] private global::Memories.Gameplay.Triggers.SceneTrigger entranceTrigger;
        private GameplayModeState state;
        private Func<UniTask> enterSchool;
        private CancellationTokenSource animationCancellation;
        private bool prepared, completed;
        private bool corridorOpened;
        public Transform CorridorEntrance => corridorEntrance;
        public bool IsRunning { get; private set; }
        public bool IsCompleted => completed;
        public string StateKey => "school-gym-entry";

        public void Initialize(GameplayModeState mode, Func<UniTask> entry)
        {
            state = mode;
            enterSchool = entry;
        }

        // Вызывается один раз явным действием предзагрузки из гаража после восстановления объектов.
        public void PrepareArrival()
        {
            if (prepared) return;
            completed = false;
            prepared = true;
            SetGarageOpeningActive(true);
            closedDoor.SetActive(false);
            openDoor.SetActive(true);
            // Временная видимость компонентов не сохраняется: временно скрытую школу нельзя записывать как неактивные объекты.
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
            corridorOpened |= openDoor.GetComponent<ObjectStateTracker>().HasRestoredState && openDoor.activeSelf;
            entranceTrigger.enabled = false;
            completed = true;
            prepared = false;
            ShowSchoolAndSealDoor();
        }

        public override void Execute()
        {
            if (!prepared || completed || IsRunning || enterSchool == null) return;
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
                // Камера уже направлена от входа. Меняем активную локацию, сохраняя блокировку управления.
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
            closedDoor.SetActive(!corridorOpened);
            openDoor.SetActive(corridorOpened);
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
        // Object activity is saved separately; retain the legacy entry state for older saves.
        public string CaptureState() => completed && openDoor.activeSelf
                ? "corridor-open" : completed ? "complete" : "waiting";
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
