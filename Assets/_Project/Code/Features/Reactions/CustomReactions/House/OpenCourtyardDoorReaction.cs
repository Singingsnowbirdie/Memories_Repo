using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом TV_BaseballMemory после просмотра первого воспоминания в доме.
    /// При Initialize также восстанавливает открытый проход, если нужное воспоминание уже просмотрено.
    /// Подгружает двор, скрывает заменяемые части фасада и изображение за дверью, открывает дверь и отключает её коллайдер.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Open Courtyard Door Reaction")]
    public sealed class OpenCourtyardDoorReaction : CustomReaction
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Renderer view;
        [SerializeField] private Collider closedDoor;
        [SerializeField, Tooltip("Objects of the house exterior replaced by the loaded courtyard.")]
        private GameObject[] hideAfterCourtyardLoaded = Array.Empty<GameObject>();
        [SerializeField] private string destination = "Assets/_Project/Locations/Backyard/Backyard.unity";
        [SerializeField] private string arrivalMarker = "Вход из дома";
        [SerializeField, Tooltip("Restores an open door on load when this memory was collected. During play, opening is triggered by a custom reaction.")]

        private string requiredMemoryId;
        private Func<string, UniTask<Scene>> load;
        private bool opening, opened;
        public bool IsOpen => opened;
        public string ArrivalMarker => arrivalMarker;

        public void Initialize(Func<string, UniTask<Scene>> loader,
            MemoryCollection collection = null)
        {
            load = loader;
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
                // Восстановленное воспоминание может быть привязано, пока сессия ещё завершает загрузку сцены.
                if (!string.IsNullOrWhiteSpace(requiredMemoryId)) await UniTask.NextFrame();
                if (this == null) return;
                await load(destination);
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

    }
}
