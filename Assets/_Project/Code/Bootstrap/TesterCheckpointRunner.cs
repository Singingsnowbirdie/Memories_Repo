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

        public Transform GetSpawn(Scene scene) => PlayerSpawnPoint.Find(scene, Selected == TesterCheckpoint.HouseEntrance ? null : Selected);

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
                string checkpointScene = checkpoint == TesterCheckpoint.ParkExitOpen ? "Assets/_Project/Locations/Park.unity" :
                    checkpoint == TesterCheckpoint.SchoolCourtyardOpen ? TestLocationLauncher.SchoolPath :
                    checkpoint == TesterCheckpoint.GarageCorridorOpen ? TestLocationLauncher.GaragePath :
                    checkpoint == TesterCheckpoint.BackyardMemoryComplete ? TestLocationLauncher.BackyardPath : TestLocationLauncher.HousePath;
                await startSession(checkpointScene);
                if (checkpoint == TesterCheckpoint.HouseMemoryComplete)
                {
                    await UniTask.WaitUntil(() => SceneManager.GetSceneByPath(TestLocationLauncher.BackyardPath).isLoaded,
                        cancellationToken: shutdownToken).Timeout(TimeSpan.FromSeconds(30));
                }
                else if (checkpoint == TesterCheckpoint.BackyardMemoryComplete)
                {
                    if (!SceneManager.GetSceneByPath(TestLocationLauncher.GaragePath).isLoaded)
                        throw new InvalidOperationException("Гараж не восстановлен для контрольной точки двора.");
                }
                else if (checkpoint == TesterCheckpoint.GarageCorridorOpen)
                {
                    var gate = FindComponent<OpenGarageGateReaction>(getCurrentScene());
                    await UniTask.WaitUntil(() => gate != null && gate.IsOpen, cancellationToken: shutdownToken).Timeout(TimeSpan.FromSeconds(30));
                }
            }
            finally { IsJumping = false; }
        }
    }
}
