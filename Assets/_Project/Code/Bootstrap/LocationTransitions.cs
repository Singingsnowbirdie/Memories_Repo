using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Player;
using VContainer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    // Управляет загруженными локациями и переходами; игрок и камера остаются в Bootstrap.
    internal sealed class LocationTransitions
    {
        private readonly CancellationToken applicationToken;
        private readonly CancellationToken shutdownToken;
        private readonly Func<bool> isDisposed;
        private readonly Func<bool> isSessionBusy;
        private readonly Func<LocationLifetimeScope> getLocationScope;
        private readonly Func<SessionLifetimeScope> getSessionScope;
        private readonly Action<Scene, LocationLifetimeScope> BindDoors;
        private readonly Func<UniTask> CleanupAsync;
        private bool transitioning;
        private bool busy { get => transitioning || isSessionBusy(); set => transitioning = value; }
        private bool disposed => isDisposed();
        private LocationLifetimeScope LocationScope => getLocationScope();
        private SessionLifetimeScope SessionScope => getSessionScope();
        private bool IsRunning => LocationScope != null;
        public bool IsBusy => transitioning;
        public Scene CurrentScene { get; private set; }
        private Scene locationScene { get => CurrentScene; set => CurrentScene = value; }
        private readonly List<Scene> additionalScenes = new List<Scene>();
        public IReadOnlyList<Scene> AdditionalScenes => additionalScenes;
        private readonly Dictionary<int, ConnectionCover> connectionCovers = new Dictionary<int, ConnectionCover>();

        public LocationTransitions(CancellationToken applicationToken, CancellationToken shutdownToken,
            Func<bool> isDisposed, Func<bool> isSessionBusy,
            Func<LocationLifetimeScope> getLocationScope, Func<SessionLifetimeScope> getSessionScope,
            Action<Scene, LocationLifetimeScope> bindDoors, Func<UniTask> cleanupAsync)
        {
            this.applicationToken = applicationToken;
            this.shutdownToken = shutdownToken;
            this.isDisposed = isDisposed;
            this.isSessionBusy = isSessionBusy;
            this.getLocationScope = getLocationScope;
            this.getSessionScope = getSessionScope;
            BindDoors = bindDoors;
            CleanupAsync = cleanupAsync;
        }

        public async UniTask LoadInitialScenesAsync(string path, bool useOpenScene,
            IEnumerable<string> additionalPaths, CancellationToken cancellationToken)
        {
            // Загрузку Unity завершаем до обработки отмены, чтобы сцена оставалась доступна для очистки.
            if (!useOpenScene) await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            locationScene = SceneManager.GetSceneByPath(path);
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var additionalPath in additionalPaths)
            {
                if (string.IsNullOrWhiteSpace(additionalPath) || !Application.CanStreamedLevelBeLoaded(additionalPath))
                    throw new ArgumentException("Additional scene must be enabled in Build Settings: " + additionalPath);
                if (SceneManager.GetSceneByPath(additionalPath).isLoaded)
                    throw new InvalidOperationException("Additional scene is already loaded: " + additionalPath);
                await SceneManager.LoadSceneAsync(additionalPath, LoadSceneMode.Additive);
                additionalScenes.Add(SceneManager.GetSceneByPath(additionalPath));
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public void ActivateCurrentScene() => SceneManager.SetActiveScene(locationScene);

        public async UniTask UnloadAllAsync()
        {
            await UnloadAdditionalScenesAsync();
            if (locationScene.IsValid() && locationScene.isLoaded)
                await SceneManager.UnloadSceneAsync(locationScene);
            locationScene = default;
        }

        public void UnloadWithoutWaiting()
        {
            if (Application.isPlaying)
                foreach (var scene in additionalScenes)
                    if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                        SceneManager.UnloadSceneAsync(scene);
            additionalScenes.Clear();
            if (Application.isPlaying && locationScene.IsValid() && locationScene.isLoaded && SceneManager.sceneCount > 1)
                SceneManager.UnloadSceneAsync(locationScene);
            locationScene = default;
        }
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

        public void RestoreConnectionCovers()
        {
            foreach (var cover in connectionCovers.Values) cover.Restore();
            connectionCovers.Clear();
        }

        public bool CanChangeLocation(string scenePath) => !disposed && !busy && IsRunning &&
            !string.IsNullOrWhiteSpace(scenePath) && Application.CanStreamedLevelBeLoaded(scenePath) &&
            !SceneManager.GetSceneByPath(scenePath).isLoaded;

        internal static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        public async UniTask ChangeLocationAsync(string scenePath)
        {
            if (!CanChangeLocation(scenePath))
                throw new InvalidOperationException("Cannot transition to location: " + scenePath);
            busy = true;
            Scene nextScene = default;
            bool committed = false;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationToken, shutdownToken);
            try
            {
                await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                nextScene = SceneManager.GetSceneByPath(scenePath);
                cancellation.Token.ThrowIfCancellationRequested();
                DisableLocationCameras(nextScene);
                BindDoors(nextScene, LocationScope);
                // Локации используют общие мировые координаты; игрок и камера в Bootstrap остаются на месте.
                var previous = locationScene;
                locationScene = nextScene;
                SceneManager.SetActiveScene(nextScene);
                committed = true;
                SessionScope.Container.Resolve<ObjectStateStore>().UnloadScene(previous);
                await SceneManager.UnloadSceneAsync(previous);
                await UnloadAdditionalScenesAsync();
                Physics.SyncTransforms();
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

        internal static Transform FindMarker(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                    if (candidate.name == name) return candidate;
            return null;
        }

        private static void ValidateConnection(Transform entrance, Transform exit)
        {
            // Локации созданы в общих мировых координатах. Загруженную статическую геометрию не перемещаем.
            if (Vector3.Distance(entrance.position, exit.position) > .01f ||
                Quaternion.Angle(entrance.rotation, exit.rotation) > .1f)
                throw new InvalidOperationException("Scene entrances must be aligned in the Editor: " +
                    exit.gameObject.scene.name + "/" + exit.name + " -> " +
                    entrance.gameObject.scene.name + "/" + entrance.name);
        }

        private static void DisableLocationCameras(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            }
        }

        public async UniTask LoadBackyardGarageAsync(BackyardGarageAttachment attachment)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationToken,
                shutdownToken, attachment.GetCancellationTokenOnDestroy());
            // Восстановленный двор может оставаться присоединённым к дому, пока игрок не войдёт в него.
            // Ждём завершения входа во двор, чтобы этот переход не выгрузил гараж.
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
                ValidateConnection(entrance, attachment.transform);
                DisableLocationCameras(garage);
                BindDoors(garage, LocationScope);
                hinge.localRotation *= Quaternion.Euler(0, 100, 0);
                Physics.SyncTransforms();
                additionalScenes.Add(garage);
                // Оставляем Backyard активной сценой, а игрока в Bootstrap — на месте.
            }
            catch
            {
                if (garage.IsValid() && garage.isLoaded) await SceneManager.UnloadSceneAsync(garage);
                throw;
            }
            finally { busy = false; }
        }

        public async UniTask<Scene> LoadGarageSchoolAsync(GarageSchoolConnection connection)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationToken,
                shutdownToken, connection.GetCancellationTokenOnDestroy());
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
                ValidateConnection(entrance.CorridorEntrance, connection.transform);
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

        public async UniTask<Scene> LoadConnectedSceneAsync(string path, OpenCourtyardDoorReaction door)
        {
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.isLoaded) return existing;
            if (disposed || busy || !Application.CanStreamedLevelBeLoaded(path))
                throw new InvalidOperationException("Cannot open connected scene: " + path);
            busy = true;
            Scene scene = default;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationToken, shutdownToken);
            try
            {
                await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                scene = SceneManager.GetSceneByPath(path);
                cancellation.Token.ThrowIfCancellationRequested();
                var arrival = FindMarker(scene, door.ArrivalMarker);
                if (arrival == null) throw new InvalidOperationException("Missing entrance marker: " + door.ArrivalMarker);
                ValidateConnection(arrival, door.transform);
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
                var backyardEntrance = FindComponent<BackyardEntranceReaction>(scene);
                if (backyardEntrance != null)
                {
                    // Контакт с триггером начинается только после того, как капсула игрока минует старый фасад.
                    var trigger = backyardEntrance.EntranceTrigger;
                    trigger.transform.SetPositionAndRotation(
                        door.transform.position + door.transform.forward * (clearance + controller.radius + .2f),
                        door.transform.rotation);
                }
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

        public async UniTask EnterGarageAsync(Scene scene)
        {
            if (disposed || busy || !additionalScenes.Contains(scene))
                throw new InvalidOperationException("Cannot enter the attached garage.");
            busy = true;
            try { await ReplaceConnectedHouseAsync(scene); }
            finally { busy = false; }
        }

        public async UniTask EnterConnectedSceneAsync(Scene scene)
        {
            if (disposed || busy || !additionalScenes.Contains(scene)) return;
            busy = true;
            try
            {
                await ReplaceConnectedHouseAsync(scene);
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
            // Восстанавливаем перекрывающуюся геометрию двора только после выгрузки дома.
            if (connectionCovers.TryGetValue(scene.handle, out var cover))
            {
                cover.Restore();
                connectionCovers.Remove(scene.handle);
            }
            Physics.SyncTransforms();
            if (disposed) await CleanupAsync();
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
    }
}
