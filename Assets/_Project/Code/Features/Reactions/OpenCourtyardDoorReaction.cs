using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Open Courtyard Door Reaction")]
    public sealed class OpenCourtyardDoorReaction : CustomReaction
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Renderer view;
        [SerializeField] private Collider closedDoor;
        [SerializeField, Tooltip("Objects of the house exterior replaced by the loaded courtyard.")]
        private GameObject[] hideAfterCourtyardLoaded = Array.Empty<GameObject>();
        [SerializeField] private string destination = "Assets/_Project/Locations/CourtyardIsland/CourtyardIsland.unity";
        [SerializeField] private string arrivalMarker = "Вход из дома";
        [SerializeField, Tooltip("Restores an open door on load when this memory was collected. During play, opening is triggered by a custom reaction.")]

        private string requiredMemoryId;
        private Func<string, UniTask<Scene>> load;
        private Func<Scene, UniTask> enter;
        private PlayerMotor player;
        private Scene courtyard;
        private bool opening, opened, crossing, passedDoorway;
        public bool IsOpen => opened;
        public string ArrivalMarker => arrivalMarker;
        public float ExitClearance { get; set; } = .6f;

        public void Initialize(PlayerMotor motor, Func<string, UniTask<Scene>> loader,
            Func<Scene, UniTask> traveller, MemoryCollection collection = null)
        {
            player = motor; load = loader; enter = traveller;
            if (collection != null && !string.IsNullOrWhiteSpace(requiredMemoryId) && collection.Contains(requiredMemoryId))
                Execute();
        }

        public override void Execute()
        {
            if (!opening && !opened && load != null)
            {
                opening = true;
                OpenAsync().Forget();
            }
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
                foreach (var target in hideAfterCourtyardLoaded)
                    if (target != null) target.SetActive(false);
                if (view != null) view.enabled = false;
                hinge.localRotation *= Quaternion.Euler(0, -100, 0);
                closedDoor.enabled = false;
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
            if (!opened || crossing || player == null) return;
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            float distance = Vector3.Dot(player.transform.position - transform.position, transform.forward);
            if (distance < 0) passedDoorway = false;
            else if (Mathf.Abs(local.x) < .65f) passedDoorway = true;
            if (passedDoorway && distance >= ExitClearance)
            {
                crossing = true;
                enter(courtyard).Forget();
            }
        }
    }
}
