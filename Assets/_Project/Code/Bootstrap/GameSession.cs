using Memories.Gameplay.Reactions;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Player;
using VContainer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    // Composition and scene ownership only. Gameplay depends on its own contracts.
    public sealed class GameSession : IDisposable, ILocationTransition
    {
        private readonly AppLifetimeScope applicationScope;
        private readonly ApplicationContext application;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private Scene locationScene;
        private readonly List<Scene> additionalScenes = new List<Scene>();
        private bool busy;
        private bool disposed;
        private TesterCheckpoint? testerCheckpoint;
        private TesterCheckpointCatalog checkpointCatalog;
        private bool jumpingToCheckpoint;
        private readonly LocationProgressStore progress = new LocationProgressStore();
        private bool trackHouseCheckpoint;
        private Transform houseEntrance;
        private Vector3 previousEntrancePosition;
        private readonly Dictionary<int, ConnectionCover> connectionCovers = new Dictionary<int, ConnectionCover>();

        private sealed class ConnectionCover
        {
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public readonly List<Collider> Colliders = new List<Collider>();
            public readonly List<GameObject> TemporaryObjects = new List<GameObject>();
            public void Restore()
            {
                foreach (var renderer in Renderers) if (renderer != null) renderer.enabled = true;
                foreach (var collider in Colliders) if (collider != null) collider.enabled = true;
                foreach (var target in TemporaryObjects) if (target != null) target.SetActive(false);
            }
        }

        private void RestoreConnectionCovers()
        {
            foreach (var cover in connectionCovers.Values) cover.Restore();
            connectionCovers.Clear();
        }

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
                    LocationScope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>().Close();
                    LocationScope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>().Stop();
                    LocationScope.Container.Resolve<Memories.Gameplay.UI.PausePresenter>().Resume();
                    LocationScope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>().Exit(Memories.Gameplay.UI.GameplayMode.Tester);
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

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
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
            // The house and highway share a player until the next doorway transition.
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
                        SessionScope.Container.Resolve<QuestInventory>(), SessionScope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
                // Unity scene loading cannot be interrupted safely. Finish the operation,
                // then honour cancellation and unload the scene we own.
                if (!useOpenScene) await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                locationScene = SceneManager.GetSceneByPath(scenePath);
                cancellation.Token.ThrowIfCancellationRequested();

                foreach (var additionalPath in resumeHouse || testerCheckpoint.HasValue ? Array.Empty<string>() : applicationScope.AdditionalLocationScenes)
                {
                    if (string.IsNullOrWhiteSpace(additionalPath) || !Application.CanStreamedLevelBeLoaded(additionalPath))
                        throw new ArgumentException("Additional scene must be enabled in Build Settings: " + additionalPath);
                    if (SceneManager.GetSceneByPath(additionalPath).isLoaded)
                        throw new InvalidOperationException("Additional scene is already loaded: " + additionalPath);
                    await SceneManager.LoadSceneAsync(additionalPath, LoadSceneMode.Additive);
                    additionalScenes.Add(SceneManager.GetSceneByPath(additionalPath));
                    cancellation.Token.ThrowIfCancellationRequested();
                }

                if (!testerCheckpoint.HasValue) applicationScope.TestLauncher?.Prepare(locationScene);
                if (resumeHouse || (testerCheckpoint.HasValue && scenePath == TestLocationLauncher.HousePath)) PrepareHouseStart(locationScene);
                if (testerCheckpoint == TesterCheckpoint.HouseMemoryComplete)
                {
                    var spawn = FindMarker(locationScene, "PlayerSpawn");
                    spawn.SetPositionAndRotation(checkpointCatalog.houseMemoryPosition, Quaternion.Euler(0, checkpointCatalog.houseMemoryYaw, 0));
                }
                else if (testerCheckpoint == TesterCheckpoint.BackyardMemoryComplete)
                    FindMarker(locationScene, "PlayerSpawn").SetPositionAndRotation(new Vector3(0, .05f, 17), Quaternion.identity);
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
                foreach (var additional in additionalScenes) BindDoors(additional, LocationScope);
                SceneManager.SetActiveScene(locationScene);
            }
            catch
            {
                await CleanupAsync();
                throw;
            }
            finally { busy = false; }
        }

        public bool CanChangeLocation(string scenePath) => !disposed && !busy && IsRunning &&
            !string.IsNullOrWhiteSpace(scenePath) && Application.CanStreamedLevelBeLoaded(scenePath) &&
            !SceneManager.GetSceneByPath(scenePath).isLoaded;

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
            // The highway is not loaded when continuing from the house checkpoint.
            var boundary = new GameObject("House start boundary");
            SceneManager.MoveGameObjectToScene(boundary, scene);
            boundary.transform.position = new Vector3(0, 2.5f, 299.5f);
            boundary.AddComponent<BoxCollider>().size = new Vector3(10, 5, 1);
        }

        public async UniTask ChangeLocationAsync(string scenePath)
        {
            if (!CanChangeLocation(scenePath))
                throw new InvalidOperationException("Cannot transition to location: " + scenePath);
            busy = true;
            Scene nextScene = default;
            bool committed = false;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(application.Token, shutdown.Token);
            try
            {
                await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                nextScene = SceneManager.GetSceneByPath(scenePath);
                cancellation.Token.ThrowIfCancellationRequested();
                var spawn = FindMarker(nextScene, "PlayerSpawn");
                if (spawn == null) throw new InvalidOperationException("Missing PlayerSpawn: " + scenePath);
                var player = LocationScope.Container.Resolve<PlayerMotor>();
                AlignScene(nextScene, spawn, player.transform.position, player.transform.rotation);
                DisableLocationCameras(nextScene);
                BindDoors(nextScene, LocationScope);
                var previous = locationScene;
                locationScene = nextScene;
                SceneManager.SetActiveScene(nextScene);
                committed = true;
                SessionScope.Container.Resolve<ObjectStateStore>().UnloadScene(previous);
                await SceneManager.UnloadSceneAsync(previous);
                await UnloadAdditionalScenesAsync();
                if (disposed) await CleanupAsync();
            }
            catch
            {
                if (!committed && nextScene.IsValid() && nextScene.isLoaded)
                    await SceneManager.UnloadSceneAsync(nextScene);
                if (disposed) await CleanupAsync();
                throw;
            }
            finally { busy = false; }
        }

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
                foreach (var connection in root.GetComponentsInChildren<GarageSchoolConnection>(true))
                    connection.Initialize(LoadGarageSchoolAsync);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<SchoolGymEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<PlayerMovementModel>(),
                        scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(), () => EnterConnectedSceneAsync(scene));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageFatherMemoryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<GarageEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<PlayerMovementModel>(),
                        scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        () => EnterGarageAsync(scene), scene == locationScene);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<BackyardEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<PlayerMovementModel>(),
                        scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        () => ReplaceConnectedHouseAsync(scene), scene == locationScene);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var attachment in root.GetComponentsInChildren<BackyardGarageAttachment>(true))
                    attachment.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>(), LoadBackyardGarageAsync);
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
                        scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>());
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), this, scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>());
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var door in root.GetComponentsInChildren<OpenCourtyardDoorReaction>(true))
                    door.Initialize(scope.Container.Resolve<PlayerMotor>(),
                        path => LoadConnectedSceneAsync(path, door), EnterConnectedSceneAsync,
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

        private static Transform FindMarker(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                    if (candidate.name == name) return candidate;
            return null;
        }

        public static void AlignScene(Scene scene, Transform marker, Vector3 position, Quaternion rotation)
        {
            var origin = marker.position;
            var delta = rotation * Quaternion.Inverse(marker.rotation);
            foreach (var root in scene.GetRootGameObjects())
                root.transform.SetPositionAndRotation(position + delta * (root.transform.position - origin),
                    delta * root.transform.rotation);
            Physics.SyncTransforms();
        }

        private static void DisableLocationCameras(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
        }

        private async UniTask LoadBackyardGarageAsync(BackyardGarageAttachment attachment)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(application.Token,
                shutdown.Token, attachment.GetCancellationTokenOnDestroy());
            // A restored yard can still be attached to the house until the player enters it.
            // Wait for yard ownership so that completing that entry cannot unload the garage.
            await UniTask.WaitUntil(() => disposed || (!busy && attachment != null &&
                locationScene == attachment.gameObject.scene), cancellationToken: cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (disposed || attachment == null) throw new OperationCanceledException();
            const string path = TestLocationLauncher.GaragePath;
            if (!Application.CanStreamedLevelBeLoaded(path))
                throw new InvalidOperationException("Garage must be enabled in Build Settings.");
            if (SceneManager.GetSceneByPath(path).isLoaded)
            {
                if (additionalScenes.Contains(SceneManager.GetSceneByPath(path))) return;
                throw new InvalidOperationException("Garage is already loaded outside this session.");
            }
            busy = true;
            Scene garage = default;
            try
            {
                await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                garage = SceneManager.GetSceneByPath(path);
                cancellation.Token.ThrowIfCancellationRequested();
                var entrance = FindMarker(garage, "Вход из двора");
                var hinge = FindMarker(garage, "Петля боковой двери");
                if (entrance == null || hinge == null)
                    throw new InvalidOperationException("Garage side entrance is not configured.");
                AlignScene(garage, entrance, attachment.transform.position, attachment.transform.rotation);
                DisableLocationCameras(garage);
                BindDoors(garage, LocationScope);
                hinge.localRotation *= Quaternion.Euler(0, 100, 0);
                Physics.SyncTransforms();
                additionalScenes.Add(garage);
                // Keep Backyard active and keep the Bootstrap player in place.
            }
            catch
            {
                if (garage.IsValid() && garage.isLoaded) await SceneManager.UnloadSceneAsync(garage);
                throw;
            }
            finally { busy = false; }
        }

        private async UniTask<Scene> LoadGarageSchoolAsync(GarageSchoolConnection connection)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(application.Token,
                shutdown.Token, connection.GetCancellationTokenOnDestroy());
            await UniTask.WaitUntil(() => disposed || (!busy && locationScene == connection.gameObject.scene),
                cancellationToken: cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (disposed) throw new OperationCanceledException();
            const string path = TestLocationLauncher.SchoolPath;
            if (!Application.CanStreamedLevelBeLoaded(path))
                throw new InvalidOperationException("School must be enabled in Build Settings.");
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.isLoaded)
            {
                if (additionalScenes.Contains(existing)) return existing;
                throw new InvalidOperationException("School is already loaded outside this session.");
            }
            busy = true;
            Scene school = default;
            try
            {
                await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                school = SceneManager.GetSceneByPath(path);
                cancellation.Token.ThrowIfCancellationRequested();
                var entrance = FindComponent<SchoolGymEntranceReaction>(school);
                if (entrance == null || entrance.CorridorEntrance == null)
                    throw new InvalidOperationException("Gym corridor entrance is not configured.");
                AlignScene(school, entrance.CorridorEntrance, connection.transform.position, connection.transform.rotation);
                DisableLocationCameras(school);
                BindDoors(school, LocationScope);
                entrance.PrepareArrival();
                additionalScenes.Add(school);
                Physics.SyncTransforms();
                return school;
            }
            catch
            {
                if (school.IsValid() && school.isLoaded) await SceneManager.UnloadSceneAsync(school);
                if (disposed) await CleanupAsync();
                throw;
            }
            finally { busy = false; }
        }

        private async UniTask<Scene> LoadConnectedSceneAsync(string path, OpenCourtyardDoorReaction door)
        {
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.isLoaded) return existing;
            if (disposed || busy || !Application.CanStreamedLevelBeLoaded(path))
                throw new InvalidOperationException("Cannot open connected scene: " + path);
            busy = true;
            Scene scene = default;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(application.Token, shutdown.Token);
            try
            {
                await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                scene = SceneManager.GetSceneByPath(path);
                cancellation.Token.ThrowIfCancellationRequested();
                var arrival = FindMarker(scene, door.ArrivalMarker);
                if (arrival == null) throw new InvalidOperationException("Missing entrance marker: " + door.ArrivalMarker);
                AlignScene(scene, arrival, door.transform.position, door.transform.rotation);
                DisableLocationCameras(scene);
                BindDoors(scene, LocationScope);
                additionalScenes.Add(scene);
                var controller = LocationScope.Container.Resolve<PlayerMotor>().GetComponent<CharacterController>();
                float clearance = controller.radius + controller.skinWidth + .05f;
                var cover = new ConnectionCover();
                if (path == TestLocationLauncher.BackyardPath)
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var t in root.GetComponentsInChildren<Transform>(true))
                            if (t.name == "Остров_6" || t.name == "Остров_5")
                            {
                                foreach (var collider in t.GetComponentsInChildren<Collider>())
                                    if (collider.enabled) { cover.Colliders.Add(collider); collider.enabled = false; }
                                foreach (var renderer in t.GetComponentsInChildren<Renderer>())
                                    if (renderer.enabled) { cover.Renderers.Add(renderer); renderer.enabled = false; }
                            }
                            else
                            if (t.name.StartsWith("Фанерный дом", StringComparison.Ordinal))
                            {
                                foreach (var collider in t.GetComponentsInChildren<Collider>())
                                {
                                    if (!collider.enabled) continue;
                                    var bounds = collider.bounds;
                                    var forward = door.transform.forward;
                                    float extent = Vector3.Dot(bounds.extents,
                                        new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z)));
                                    if (bounds.max.y > door.transform.position.y + controller.radius &&
                                        bounds.min.y < door.transform.position.y + controller.height - controller.radius)
                                        clearance = Mathf.Max(clearance,
                                            Vector3.Dot(bounds.center - door.transform.position, forward) + extent +
                                            controller.radius + controller.skinWidth + .05f);
                                    cover.Colliders.Add(collider);
                                    collider.enabled = false;
                                }
                                foreach (var renderer in t.GetComponentsInChildren<Renderer>())
                                    if (renderer.enabled) { cover.Renderers.Add(renderer); renderer.enabled = false; }
                            }
                connectionCovers[scene.handle] = cover;
                door.ExitClearance = clearance;
                return scene;
            }
            catch
            {
                additionalScenes.Remove(scene);
                if (scene.IsValid() && scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
                if (disposed) await CleanupAsync();
                throw;
            }
            finally { busy = false; }
        }

        private async UniTask EnterGarageAsync(Scene scene)
        {
            if (disposed || busy || !additionalScenes.Contains(scene))
                throw new InvalidOperationException("Cannot enter the attached garage.");
            busy = true;
            try { await ReplaceConnectedHouseAsync(scene); }
            finally { busy = false; }
        }

        private async UniTask EnterConnectedSceneAsync(Scene scene)
        {
            if (disposed || busy || !additionalScenes.Contains(scene)) return;
            busy = true;
            try
            {
                var entrance = FindComponent<BackyardEntranceReaction>(scene);
                if (entrance != null)
                {
                    entrance.Execute();
                    await entrance.Completion;
                }
                else await ReplaceConnectedHouseAsync(scene);
            }
            finally { busy = false; }
        }

        private async UniTask ReplaceConnectedHouseAsync(Scene scene)
        {
            var previous = locationScene;
            additionalScenes.Remove(scene);
            locationScene = scene;
            SceneManager.SetActiveScene(scene);
            SessionScope.Container.Resolve<ObjectStateStore>().UnloadScene(previous);
            foreach (var root in previous.GetRootGameObjects()) root.SetActive(false);
            await SceneManager.UnloadSceneAsync(previous);
            await UnloadAdditionalScenesAsync();
            // Restore overlapping backyard geometry only after the house has unloaded.
            if (connectionCovers.TryGetValue(scene.handle, out var cover))
            {
                cover.Restore();
                connectionCovers.Remove(scene.handle);
            }
            Physics.SyncTransforms();
            if (disposed) await CleanupAsync();
        }

        private async UniTask CleanupAsync()
        {
            ReleaseScopes();
            if (applicationScope != null && applicationScope.gameObject.scene.isLoaded)
                SceneManager.SetActiveScene(applicationScope.gameObject.scene);
            await UnloadAdditionalScenesAsync();
            if (locationScene.IsValid() && locationScene.isLoaded)
                await SceneManager.UnloadSceneAsync(locationScene);
            locationScene = default;
        }

        private async UniTask UnloadAdditionalScenesAsync()
        {
            foreach (var scene in additionalScenes)
                if (scene.IsValid() && scene.isLoaded)
                {
                    if (SessionScope != null) SessionScope.Container.Resolve<ObjectStateStore>().UnloadScene(scene);
                    await SceneManager.UnloadSceneAsync(scene);
                }
            additionalScenes.Clear();
        }

        private void ReleaseScopes()
        {
            RestoreConnectionCovers();
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
            // An in-flight StartAsync owns cleanup after its scene load finishes.
            if (busy) return;
            ReleaseScopes();
            if (Application.isPlaying)
                foreach (var scene in additionalScenes)
                    if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                        SceneManager.UnloadSceneAsync(scene);
            additionalScenes.Clear();
            if (Application.isPlaying && locationScene.IsValid() && locationScene.isLoaded
                && SceneManager.sceneCount > 1)
                SceneManager.UnloadSceneAsync(locationScene);
            locationScene = default;
        }
    }
}
