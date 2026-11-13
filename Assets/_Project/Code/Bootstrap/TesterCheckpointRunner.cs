using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using static Memories.Bootstrap.LocationTransitions;

namespace Memories.Bootstrap
{
    // Готовит тестовый прогресс, выбирает стартовую позицию и запускает контрольную точку.
    internal sealed class TesterCheckpointRunner
    {
        private readonly Func<LocationLifetimeScope> getLocationScope;
        private readonly Func<Scene> getCurrentScene;
        private readonly Func<bool> canJump;
        private readonly Func<UniTask> prepareSession;
        private readonly Func<string, UniTask> startSession;
        private readonly CancellationToken shutdownToken;
        public TesterCheckpoint? Selected { get; private set; }
        public TesterCheckpointCatalog Catalog { get; private set; }
        public bool IsJumping { get; private set; }
        public bool HasSelection => Selected.HasValue;

        public TesterCheckpointRunner(Func<LocationLifetimeScope> getLocationScope, Func<Scene> getCurrentScene,
            Func<bool> canJump, Func<UniTask> prepareSession, Func<string, UniTask> startSession,
            CancellationToken shutdownToken)
        {
            this.getLocationScope = getLocationScope;
            this.getCurrentScene = getCurrentScene;
            this.canJump = canJump;
            this.prepareSession = prepareSession;
            this.startSession = startSession;
            this.shutdownToken = shutdownToken;
        }

        public void Seed(SessionLifetimeScope scope)
        {
            if (!Selected.HasValue) return;
            Catalog.Seed(Selected.Value, scope.Container.Resolve<CustomVariableStore>(),
                scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<Gameplay.Memories.MemoryCollection>());
        }

        public void PrepareSpawn(Scene scene)
        {
            if (Selected == TesterCheckpoint.HouseMemoryComplete)
            {
                var spawn = FindMarker(scene, "PlayerSpawn");
                spawn.SetPositionAndRotation(Catalog.houseMemoryPosition, Quaternion.Euler(0, Catalog.houseMemoryYaw, 0));
            }
            else if (Selected == TesterCheckpoint.BackyardMemoryComplete)
            {
                var entrance = FindMarker(scene, "Вход из дома");
                FindMarker(scene, "PlayerSpawn").SetPositionAndRotation(
                    entrance.TransformPoint(new Vector3(0, .05f, 16.84f)), entrance.rotation);
            }
            else if (Selected == TesterCheckpoint.SchoolCourtyardOpen)
            {
                var corridor = FindMarker(scene, "Школьный коридор");
                var spawn = FindMarker(scene, "PlayerSpawn");
                var position = corridor.TransformPoint(new Vector3(0, .05f, 33.8f));

                spawn.SetPositionAndRotation(position, corridor.rotation);
            }
            else if (Selected == TesterCheckpoint.GarageCorridorOpen)
            {
                var exit = FindComponent<GarageSchoolConnection>(scene).transform;
                FindMarker(scene, "PlayerSpawn").SetPositionAndRotation(
                    exit.position - exit.forward * 1.5f + Vector3.up * .05f, exit.rotation);
            }
        }

        public async UniTask JumpToCheckpointAsync(TesterCheckpoint checkpoint)
        {
            if (!canJump() || IsJumping) throw new InvalidOperationException("Дождитесь завершения загрузки.");
            if (!Enum.IsDefined(typeof(TesterCheckpoint), checkpoint)) throw new ArgumentOutOfRangeException(nameof(checkpoint));
            var catalog = Resources.Load<TesterCheckpointCatalog>("TesterCheckpoints");
            if (catalog == null) throw new InvalidOperationException("Не найдены настройки контрольных точек.");
            IsJumping = true;
            try
            {
                if (getLocationScope() != null)
                {
                    getLocationScope().Container.Resolve<Gameplay.Memories.MemoryPresenter>().Close();
                    getLocationScope().Container.Resolve<Gameplay.UI.StoryTextPresenter>().Stop();
                    getLocationScope().Container.Resolve<Gameplay.UI.PausePresenter>().Resume();
                    getLocationScope().Container.Resolve<Gameplay.UI.GameplayModeState>().Exit(Gameplay.UI.GameplayMode.Tester);
                }
                await prepareSession();
                Selected = checkpoint;
                Catalog = catalog;
                string checkpointScene = checkpoint == TesterCheckpoint.SchoolCourtyardOpen ? TestLocationLauncher.SchoolPath :
                    checkpoint == TesterCheckpoint.GarageCorridorOpen ? TestLocationLauncher.GaragePath :
                    checkpoint == TesterCheckpoint.BackyardMemoryComplete ? TestLocationLauncher.BackyardPath : TestLocationLauncher.HousePath;
                await startSession(checkpointScene);
                if (checkpoint == TesterCheckpoint.HouseMemoryComplete)
                {
                    var door = FindComponent<OpenCourtyardDoorReaction>(getCurrentScene());
                    await UniTask.WaitUntil(() => door != null && door.IsOpen, cancellationToken: shutdownToken).Timeout(TimeSpan.FromSeconds(30));
                }
                else if (checkpoint == TesterCheckpoint.BackyardMemoryComplete)
                {
                    var garage = FindComponent<BackyardGarageAttachment>(getCurrentScene());
                    await UniTask.WaitUntil(() => garage != null && garage.IsOpen, cancellationToken: shutdownToken).Timeout(TimeSpan.FromSeconds(30));
                }
                else if (checkpoint == TesterCheckpoint.GarageCorridorOpen)
                {
                    var connection = FindComponent<GarageSchoolConnection>(getCurrentScene());
                    await connection.EnsureReadyAsync();
                    var gate = FindComponent<OpenGarageGateReaction>(getCurrentScene());
                    await UniTask.WaitUntil(() => gate != null && gate.IsOpen, cancellationToken: shutdownToken).Timeout(TimeSpan.FromSeconds(30));
                }
            }
            finally { IsJumping = false; }
        }
    }
}
