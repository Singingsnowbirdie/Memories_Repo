using Memories.Gameplay.Reactions;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Player;
using VContainer;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Memories.Bootstrap.LocationTransitions;

namespace Memories.Bootstrap
{
    // Только сборка зависимостей и управление сценами. Игровая логика зависит от собственных контрактов.
    public sealed class GameSession : IDisposable, ILocationTransition
    {
        private readonly AppLifetimeScope applicationScope;
        private readonly ApplicationContext application;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly LocationTransitions transitions;
        private readonly LocationBinder binder;
        private Scene locationScene => transitions.CurrentScene;

        private bool lifecycleBusy;
        private bool busy { get => lifecycleBusy || transitions.IsBusy; set => lifecycleBusy = value; }
        private bool disposed;
        private readonly TesterCheckpointRunner checkpoints;
        private readonly LocationProgressStore progress = new LocationProgressStore();
        private bool trackHouseCheckpoint;
        private Transform houseEntrance;
        private Vector3 previousEntrancePosition;

        public UniTask StartSavedGameAsync(string scenePath, CancellationToken cancellationToken = default)
        {
            bool useProgress = applicationScope.TestLauncher == null && scenePath == TestLocationLauncher.HighwayPath;
#if UNITY_EDITOR
            useProgress &= !applicationScope.UseOpenLocationForEditorPlay;
#endif
            bool resumeHouse = useProgress && progress.HasReachedHouse;
            trackHouseCheckpoint = useProgress && !resumeHouse;
            return StartAsync(resumeHouse ? TestLocationLauncher.HousePath : scenePath, cancellationToken,
                resumeHouse);
        }

        public void UpdateHouseCheckpoint()
        {
            if (!trackHouseCheckpoint || busy || disposed || !IsRunning) return;
            var house = SceneManager.GetSceneByPath(TestLocationLauncher.HousePath);
            if (!house.isLoaded) return;
            var player = LocationScope.Container.Resolve<PlayerMotor>();
            if (houseEntrance == null)
            {
                foreach (var root in house.GetRootGameObjects())
                    if (root.name == "Дом — два этажа") houseEntrance = root.transform;
                if (houseEntrance == null) return;
                previousEntrancePosition = houseEntrance.InverseTransformPoint(player.transform.position);
                return;
            }
            var current = houseEntrance.InverseTransformPoint(player.transform.position);
            if (HouseCheckpointEntryPoint.CrossedEntrance(previousEntrancePosition, current))
            {
                progress.SaveHouse();
                trackHouseCheckpoint = false;
            }
            previousEntrancePosition = current;
        }

        public SessionLifetimeScope SessionScope { get; private set; }
        public LocationLifetimeScope LocationScope { get; private set; }
        public Scene CurrentLocationScene => locationScene;
        public bool IsRunning => LocationScope != null;
        public bool IsTransitioning => busy || checkpoints.IsJumping;

        public UniTask JumpToCheckpointAsync(TesterCheckpoint checkpoint) => checkpoints.JumpToCheckpointAsync(checkpoint);

        private async UniTask PrepareTesterSessionAsync()
        {
            busy = true;
            try { await CleanupAsync(); }
            finally { busy = false; }
            applicationScope.IsTesterSession = true;
            trackHouseCheckpoint = false;
        }
        public bool RestartGame()
        {
            const string bootstrap = "Assets/_Project/Scenes/Bootstrap.unity";
            if (disposed || busy || !IsRunning || !Application.CanStreamedLevelBeLoaded(bootstrap)) return false;
            var loading = SceneManager.LoadSceneAsync(bootstrap, LoadSceneMode.Single);
            if (loading == null) return false;
            trackHouseCheckpoint = false;
            busy = true;
            SessionScope.Container.Resolve<QuestInventorySave>().Dispose();
            SessionScope.Container.Resolve<ObjectStateStore>().Reset();
            if (applicationScope.PersistProgress)
            {
                progress.Reset();
                QuestInventorySave.Clear();
                CustomVariableStore.Clear();
                RevealPhotoReaction.ResetProgress();
                Memories.Gameplay.Memories.MemoryCollection.Clear();
            }
            return true;
        }

        public bool ReturnToLocationStart()
        {
            if (disposed || busy || !IsRunning) return false;
            var player = LocationScope.Container.Resolve<PlayerMotor>();
            var spawn = FindMarker(locationScene, "PlayerSpawn");
            var position = spawn != null ? spawn.position : player.InitialPosition;
            float yaw = spawn != null ? spawn.eulerAngles.y : player.InitialYaw;
            // Дом и шоссе используют одного игрока до следующего перехода через дверной проём.
            var house = SceneManager.GetSceneByPath(TestLocationLauncher.HousePath);
            if (house.isLoaded && (locationScene == house ||
                (locationScene.path == TestLocationLauncher.HighwayPath && player.transform.position.z >= 300f)))
            {
                position = new Vector3(0, .05f, 305f);
                yaw = 0;
            }
            player.Teleport(position, yaw);
            LocationScope.Container.Resolve<PlayerMovementModel>().ResetMotion(yaw);
            return true;
        }

        public GameSession(AppLifetimeScope applicationScope, ApplicationContext application)
        {
            this.applicationScope = applicationScope;
            this.application = application;
            transitions = new LocationTransitions(application.Token, shutdown.Token,
                () => disposed, () => lifecycleBusy, () => LocationScope, () => SessionScope,
                BindLocation, CleanupAsync);
            binder = new LocationBinder(transitions, this);
            checkpoints = new TesterCheckpointRunner(() => LocationScope, () => locationScene,
                () => !disposed && !busy, PrepareTesterSessionAsync, path => StartAsync(path), shutdown.Token);
        }

        public async UniTask StartAsync(string scenePath, CancellationToken cancellationToken = default,
            bool resumeHouse = false)
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameSession));
            if (busy || SessionScope != null) throw new InvalidOperationException("A session is already running or transitioning.");
            cancellationToken.ThrowIfCancellationRequested();
            bool useOpenScene = false;
#if UNITY_EDITOR
            useOpenScene = !checkpoints.HasSelection && applicationScope.UseOpenLocationForEditorPlay && SceneManager.GetSceneByPath(scenePath).isLoaded;
#endif
            if (string.IsNullOrWhiteSpace(scenePath) || (!useOpenScene && !Application.CanStreamedLevelBeLoaded(scenePath)))
                throw new ArgumentException("Location must be enabled in Build Settings: " + scenePath, nameof(scenePath));
            if (!useOpenScene && SceneManager.GetSceneByPath(scenePath).isLoaded)
                throw new InvalidOperationException("Location is already loaded. Start the game from Bootstrap.");

            busy = true;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, application.Token, shutdown.Token);
            try
            {
                // Контрольные точки обходят вступление: прицел доступен сразу, без ожидания сюжетного текста.
                Memories.Gameplay.UI.StoryTextView.BeginSession(useOpenScene || checkpoints.HasSelection);
                SessionScope = applicationScope.CreateChild<SessionLifetimeScope>(childScopeName: "SessionLifetimeScope");
                checkpoints.Seed(SessionScope);
                await transitions.LoadInitialScenesAsync(scenePath, useOpenScene,
                    resumeHouse || checkpoints.HasSelection ? Array.Empty<string>() : applicationScope.AdditionalLocationScenes,
                    cancellation.Token);

                if (!checkpoints.HasSelection) applicationScope.TestLauncher?.Prepare(locationScene);
                if (resumeHouse || (checkpoints.HasSelection && scenePath == TestLocationLauncher.HousePath)) PrepareHouseStart(locationScene);
                checkpoints.PrepareSpawn(locationScene);
                var root = new GameObject("Persistent gameplay");
                root.SetActive(false);
                root.transform.SetParent(SessionScope.transform, false);
                LocationScope = root.AddComponent<LocationLifetimeScope>();
                LocationScope.InitialScene = locationScene;
                LocationScope.parentReference.Object = SessionScope;
                root.SetActive(true);
                BindLocation(locationScene, LocationScope);
                foreach (var additional in transitions.AdditionalScenes) BindLocation(additional, LocationScope);
                transitions.ActivateCurrentScene();
            }
            catch
            {
                await CleanupAsync();
                throw;
            }
            finally { busy = false; }
        }

        public bool CanChangeLocation(string scenePath) => transitions.CanChangeLocation(scenePath);

        private static void PrepareHouseStart(Scene scene)
        {
            var spawn = new GameObject("PlayerSpawn");
            SceneManager.MoveGameObjectToScene(spawn, scene);
            spawn.transform.position = new Vector3(0, .05f, 305);
            var view = new GameObject("House start camera");
            SceneManager.MoveGameObjectToScene(view, scene);
            view.tag = "MainCamera";
            view.transform.position = spawn.transform.position + Vector3.up * 1.65f;
            var camera = view.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000;
            view.AddComponent<AudioListener>();
            // При продолжении с контрольной точки дома шоссе не загружается.
            var boundary = new GameObject("House start boundary");
            SceneManager.MoveGameObjectToScene(boundary, scene);
            boundary.transform.position = new Vector3(0, 2.5f, 299.5f);
            boundary.AddComponent<BoxCollider>().size = new Vector3(10, 5, 1);
        }

        public UniTask ChangeLocationAsync(string scenePath) => transitions.ChangeLocationAsync(scenePath);

        public async UniTask EndAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameSession));
            if (busy) throw new InvalidOperationException("A session transition is already in progress.");
            busy = true;
            try { await CleanupAsync(); }
            finally { busy = false; }
        }

        private void BindLocation(Scene scene, LocationLifetimeScope scope) =>
            binder.Bind(scene, scope, scene == locationScene, checkpoints.Selected, checkpoints.Catalog);

        private async UniTask CleanupAsync()
        {
            ReleaseScopes();
            if (applicationScope != null && applicationScope.gameObject.scene.isLoaded)
                SceneManager.SetActiveScene(applicationScope.gameObject.scene);
            await transitions.UnloadAllAsync();
        }

        private void ReleaseScopes()
        {
            transitions.RestoreConnectionCovers();
            if (SessionScope != null) SessionScope.Container.Resolve<ObjectStateStore>().Flush();
            if (LocationScope != null) LocationScope.Dispose();
            LocationScope = null;
            if (SessionScope != null) SessionScope.Dispose();
            SessionScope = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            shutdown.Cancel();
            shutdown.Dispose();
            // Выполняющийся StartAsync отвечает за очистку после завершения загрузки своей сцены.
            if (busy) return;
            ReleaseScopes();
            transitions.UnloadWithoutWaiting();
        }
    }
}
