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
        private Scene locationScene => transitions.CurrentScene;

        private bool lifecycleBusy;
        private bool busy { get => lifecycleBusy || transitions.IsBusy; set => lifecycleBusy = value; }
        private bool disposed;
        private TesterCheckpoint? testerCheckpoint;
        private TesterCheckpointCatalog checkpointCatalog;
        private bool jumpingToCheckpoint;
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
        public bool IsTransitioning => busy || jumpingToCheckpoint;

        public async UniTask JumpToCheckpointAsync(TesterCheckpoint checkpoint)
        {
            if (disposed || busy || jumpingToCheckpoint) throw new InvalidOperationException("Дождитесь завершения загрузки.");
            if (!Enum.IsDefined(typeof(TesterCheckpoint), checkpoint)) throw new ArgumentOutOfRangeException(nameof(checkpoint));
            var catalog = Resources.Load<TesterCheckpointCatalog>("TesterCheckpoints");
            if (catalog == null) throw new InvalidOperationException("Не найдены настройки контрольных точек.");
            jumpingToCheckpoint = true;
            try
            {
                if (LocationScope != null)
                {
                    LocationScope.Container.Resolve<Gameplay.Memories.MemoryPresenter>().Close();
                    LocationScope.Container.Resolve<Gameplay.UI.StoryTextPresenter>().Stop();
                    LocationScope.Container.Resolve<Gameplay.UI.PausePresenter>().Resume();
                    LocationScope.Container.Resolve<Gameplay.UI.GameplayModeState>().Exit(Gameplay.UI.GameplayMode.Tester);
                }
                busy = true;
                try { await CleanupAsync(); }
                finally { busy = false; }
                applicationScope.IsTesterSession = true;
                trackHouseCheckpoint = false;
                testerCheckpoint = checkpoint;
                checkpointCatalog = catalog;
                string checkpointScene = checkpoint == TesterCheckpoint.GarageCorridorOpen ? TestLocationLauncher.GaragePath :
                    checkpoint == TesterCheckpoint.BackyardMemoryComplete ? TestLocationLauncher.BackyardPath : TestLocationLauncher.HousePath;
                await StartAsync(checkpointScene);
                if (checkpoint == TesterCheckpoint.HouseMemoryComplete)
                {
                    var door = FindComponent<OpenCourtyardDoorReaction>(locationScene);
                    await UniTask.WaitUntil(() => door != null && door.IsOpen, cancellationToken: shutdown.Token).Timeout(TimeSpan.FromSeconds(30));
                }
                else if (checkpoint == TesterCheckpoint.BackyardMemoryComplete)
                {
                    var garage = FindComponent<BackyardGarageAttachment>(locationScene);
                    await UniTask.WaitUntil(() => garage != null && garage.IsOpen, cancellationToken: shutdown.Token).Timeout(TimeSpan.FromSeconds(30));
                }
                else if (checkpoint == TesterCheckpoint.GarageCorridorOpen)
                {
                    var connection = FindComponent<GarageSchoolConnection>(locationScene);
                    await connection.EnsureReadyAsync();
                    var gate = FindComponent<OpenGarageGateReaction>(locationScene);
                    await UniTask.WaitUntil(() => gate != null && gate.IsOpen, cancellationToken: shutdown.Token).Timeout(TimeSpan.FromSeconds(30));
                }
            }
            finally { jumpingToCheckpoint = false; }
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
                BindDoors, CleanupAsync);
        }

        public async UniTask StartAsync(string scenePath, CancellationToken cancellationToken = default,
            bool resumeHouse = false)
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameSession));
            if (busy || SessionScope != null) throw new InvalidOperationException("A session is already running or transitioning.");
            cancellationToken.ThrowIfCancellationRequested();
            bool useOpenScene = false;
#if UNITY_EDITOR
            useOpenScene = !testerCheckpoint.HasValue && applicationScope.UseOpenLocationForEditorPlay && SceneManager.GetSceneByPath(scenePath).isLoaded;
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
                Memories.Gameplay.UI.StoryTextView.BeginSession(useOpenScene || testerCheckpoint.HasValue);
                SessionScope = applicationScope.CreateChild<SessionLifetimeScope>(childScopeName: "SessionLifetimeScope");
                if (testerCheckpoint.HasValue)
                    checkpointCatalog.Seed(testerCheckpoint.Value, SessionScope.Container.Resolve<CustomVariableStore>(),
                        SessionScope.Container.Resolve<QuestInventory>(), SessionScope.Container.Resolve<Gameplay.Memories.MemoryCollection>());
                await transitions.LoadInitialScenesAsync(scenePath, useOpenScene,
                    resumeHouse || testerCheckpoint.HasValue ? Array.Empty<string>() : applicationScope.AdditionalLocationScenes,
                    cancellation.Token);

                if (!testerCheckpoint.HasValue) applicationScope.TestLauncher?.Prepare(locationScene);
                if (resumeHouse || (testerCheckpoint.HasValue && scenePath == TestLocationLauncher.HousePath)) PrepareHouseStart(locationScene);
                if (testerCheckpoint == TesterCheckpoint.HouseMemoryComplete)
                {
                    var spawn = FindMarker(locationScene, "PlayerSpawn");
                    spawn.SetPositionAndRotation(checkpointCatalog.houseMemoryPosition, Quaternion.Euler(0, checkpointCatalog.houseMemoryYaw, 0));
                }
                else if (testerCheckpoint == TesterCheckpoint.BackyardMemoryComplete)
                {
                    var entrance = FindMarker(locationScene, "Вход из дома");
                    FindMarker(locationScene, "PlayerSpawn").SetPositionAndRotation(
                        entrance.TransformPoint(new Vector3(0, .05f, 16.84f)), entrance.rotation);
                }
                else if (testerCheckpoint == TesterCheckpoint.GarageCorridorOpen)
                {
                    var exit = FindComponent<GarageSchoolConnection>(locationScene).transform;
                    FindMarker(locationScene, "PlayerSpawn").SetPositionAndRotation(
                        exit.position - exit.forward * 1.5f + Vector3.up * .05f, exit.rotation);
                }
                var root = new GameObject("Persistent gameplay");
                root.SetActive(false);
                root.transform.SetParent(SessionScope.transform, false);
                LocationScope = root.AddComponent<LocationLifetimeScope>();
                LocationScope.InitialScene = locationScene;
                LocationScope.parentReference.Object = SessionScope;
                root.SetActive(true);
                BindDoors(locationScene, LocationScope);
                foreach (var additional in transitions.AdditionalScenes) BindDoors(additional, LocationScope);
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

        private void BindDoors(Scene scene, LocationLifetimeScope scope)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var departure in root.GetComponentsInChildren<SchoolAmbulanceDepartureReaction>(true))
                    departure.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(),
                        () => ChangeLocationAsync("Assets/_Project/Locations/Park/Park.unity"));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var phone in root.GetComponentsInChildren<RevealSchoolPhoneReaction>(true))
                    phone.Initialize(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var supplies in root.GetComponentsInChildren<SchoolFirstAidProgressReaction>(true))
                    supplies.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var connection in root.GetComponentsInChildren<GarageSchoolConnection>(true))
                    connection.Initialize(transitions.LoadGarageSchoolAsync);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<SchoolGymEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(), () => transitions.EnterConnectedSceneAsync(scene));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageFatherMemoryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<GarageEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        () => transitions.EnterGarageAsync(scene), scene == locationScene);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<BackyardEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        () => transitions.EnterConnectedSceneAsync(scene), scene == locationScene);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var attachment in root.GetComponentsInChildren<BackyardGarageAttachment>(true))
                    attachment.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>(), transitions.LoadBackyardGarageAsync);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var photo in root.GetComponentsInChildren<RevealPhotoReaction>(true))
                    photo.Restore(scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        scope.Container.Resolve<PlayerMotor>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var focus in root.GetComponentsInChildren<FocusCameraReaction>(true))
                    focus.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<CloseEntranceDoorsReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<CloseStairDoorReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.TriggerEvent>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.SceneTrigger>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), this,
                        scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<PlayerLookAction>());
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), this, scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<PlayerLookAction>());
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var door in root.GetComponentsInChildren<OpenCourtyardDoorReaction>(true))
                    door.Initialize(
                        path => transitions.LoadConnectedSceneAsync(path, door),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            scope.Container.Resolve<ObjectStateStore>().RestoreScene(scene);
            if (scene == locationScene)
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var entrance in root.GetComponentsInChildren<SchoolGymEntranceReaction>(true))
                        entrance.RestoreForDirectLoad();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.RestoreInitialVisibility(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var visibility in root.GetComponentsInChildren<SavedObjectVisibility>(true))
                    visibility.Restore(scope.Container.Resolve<CustomVariableStore>());
            if (testerCheckpoint.HasValue) checkpointCatalog.ApplyScene(testerCheckpoint.Value, scene);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageOpenHoodFatherReaction>(true))
                    reaction.Restore(scope.Container.Resolve<CustomVariableStore>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageFinalMemoryReaction>(true))
                    reaction.Restore(scope.Container.Resolve<QuestInventory>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<ShowSeatedBenReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<RemoveClockBatteryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<RemoveToyPlaneBatteryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<RefreshBaseballAreaReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
        }

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
