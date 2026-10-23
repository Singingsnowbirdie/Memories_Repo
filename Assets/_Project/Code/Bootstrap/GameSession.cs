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

        public SessionLifetimeScope SessionScope { get; private set; }
        public LocationLifetimeScope LocationScope { get; private set; }
        public bool IsRunning => LocationScope != null;

        public GameSession(AppLifetimeScope applicationScope, ApplicationContext application)
        {
            this.applicationScope = applicationScope;
            this.application = application;
        }

        public async UniTask StartAsync(string scenePath, CancellationToken cancellationToken = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(GameSession));
            if (busy || SessionScope != null) throw new InvalidOperationException("A session is already running or transitioning.");
            cancellationToken.ThrowIfCancellationRequested();
            bool useOpenScene = false;
#if UNITY_EDITOR
            useOpenScene = applicationScope.UseOpenLocationForEditorPlay && SceneManager.GetSceneByPath(scenePath).isLoaded;
#endif
            if (string.IsNullOrWhiteSpace(scenePath) || (!useOpenScene && !Application.CanStreamedLevelBeLoaded(scenePath)))
                throw new ArgumentException("Location must be enabled in Build Settings: " + scenePath, nameof(scenePath));
            if (!useOpenScene && SceneManager.GetSceneByPath(scenePath).isLoaded)
                throw new InvalidOperationException("Location is already loaded. Start the game from Bootstrap.");

            busy = true;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, application.Token, shutdown.Token);
            try
            {
                SessionScope = applicationScope.CreateChild<SessionLifetimeScope>(childScopeName: "SessionLifetimeScope");
                // Unity scene loading cannot be interrupted safely. Finish the operation,
                // then honour cancellation and unload the scene we own.
                if (!useOpenScene) await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                locationScene = SceneManager.GetSceneByPath(scenePath);
                cancellation.Token.ThrowIfCancellationRequested();

                foreach (var additionalPath in applicationScope.AdditionalLocationScenes)
                {
                    if (string.IsNullOrWhiteSpace(additionalPath) || !Application.CanStreamedLevelBeLoaded(additionalPath))
                        throw new ArgumentException("Additional scene must be enabled in Build Settings: " + additionalPath);
                    if (SceneManager.GetSceneByPath(additionalPath).isLoaded)
                        throw new InvalidOperationException("Additional scene is already loaded: " + additionalPath);
                    await SceneManager.LoadSceneAsync(additionalPath, LoadSceneMode.Additive);
                    additionalScenes.Add(SceneManager.GetSceneByPath(additionalPath));
                    cancellation.Token.ThrowIfCancellationRequested();
                }

                applicationScope.TestLauncher?.Prepare(locationScene);
                var root = new GameObject("LocationLifetimeScope");
                root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, locationScene);
                LocationScope = root.AddComponent<LocationLifetimeScope>();
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

        public async UniTask ChangeLocationAsync(string scenePath)
        {
            if (!CanChangeLocation(scenePath))
                throw new InvalidOperationException("Cannot transition to location: " + scenePath);

            busy = true;
            var previousScene = locationScene;
            var previousScope = LocationScope;
            Scene nextScene = default;
            LocationLifetimeScope nextScope = null;
            bool committed = false;
            // The transition outlives the trigger in the scene being unloaded.
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(application.Token, shutdown.Token);
            try
            {
                await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                nextScene = SceneManager.GetSceneByPath(scenePath);
                cancellation.Token.ThrowIfCancellationRequested();
                var root = new GameObject("LocationLifetimeScope");
                root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, nextScene);
                nextScope = root.AddComponent<LocationLifetimeScope>();
                nextScope.parentReference.Object = SessionScope;
                root.SetActive(true);
                BindDoors(nextScene, nextScope);

                SceneManager.SetActiveScene(nextScene);
                locationScene = nextScene;
                LocationScope = nextScope;
                committed = true;
                foreach (var previousRoot in previousScene.GetRootGameObjects()) previousRoot.SetActive(false);
                previousScope.Dispose();
                await SceneManager.UnloadSceneAsync(previousScene);
                await UnloadAdditionalScenesAsync();
                if (disposed) await CleanupAsync();
            }
            catch
            {
                if (!committed)
                {
                    nextScope?.Dispose();
                    if (nextScene.IsValid() && nextScene.isLoaded) await SceneManager.UnloadSceneAsync(nextScene);
                    if (disposed) await CleanupAsync();
                }
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
                foreach (var door in root.GetComponentsInChildren<CourtyardDoor>(true))
                    door.Initialize(scope.Container.Resolve<PlayerMotor>(),
                        path => LoadConnectedSceneAsync(path, scene), EnterConnectedSceneAsync);
        }

        private async UniTask<Scene> LoadConnectedSceneAsync(string path, Scene doorwayScene)
        {
            var existing = SceneManager.GetSceneByPath(path);
            if (existing.isLoaded) return existing;
            if (disposed || busy || !Application.CanStreamedLevelBeLoaded(path))
                throw new InvalidOperationException("Cannot open connected scene: " + path);
            busy = true;
            try
            {
                await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                var scene = SceneManager.GetSceneByPath(path);
                additionalScenes.Add(scene);
                if (disposed)
                {
                    await CleanupAsync();
                    throw new OperationCanceledException("Session closed while opening the courtyard.");
                }
                // Keep successive connected locations outside the current camera range.
                float destinationOffset = LocationScope.Container.Resolve<PlayerMotor>().transform.position.x + 2000f;
                foreach (var root in scene.GetRootGameObjects())
                {
                    root.transform.position += Vector3.right * destinationOffset;
                    foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                    foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
                    foreach (var light in root.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << 30;
                }
                // Keep the live player, camera and UI with the house before unloading
                // the highway that originally owned their lifetime scope.
                var highway = SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath);
                if (highway.isLoaded && highway != doorwayScene &&
                    (highway == locationScene || additionalScenes.Contains(highway)))
                {
                    if (highway == locationScene)
                    {
                        SceneManager.MoveGameObjectToScene(LocationScope.gameObject, doorwayScene);
                        additionalScenes.Remove(doorwayScene);
                        locationScene = doorwayScene;
                        SceneManager.SetActiveScene(doorwayScene);
                    }
                    additionalScenes.Remove(highway);
                    await SceneManager.UnloadSceneAsync(highway);
                    if (disposed)
                    {
                        await CleanupAsync();
                        throw new OperationCanceledException("Session closed while unloading the highway.");
                    }
                }
                return scene;
            }
            finally { busy = false; }
        }

        private async UniTask EnterConnectedSceneAsync(Scene scene, Vector3 position, Quaternion rotation, float pitch)
        {
            if (disposed || busy || !additionalScenes.Contains(scene)) return;
            busy = true;
            var previous = locationScene;
            var previousScope = LocationScope;
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == "PlayerSpawn") t.SetPositionAndRotation(position, rotation);
                var scopeRoot = new GameObject("LocationLifetimeScope");
                scopeRoot.SetActive(false);
                SceneManager.MoveGameObjectToScene(scopeRoot, scene);
                var scope = scopeRoot.AddComponent<LocationLifetimeScope>();
                scope.parentReference.Object = SessionScope;
                scopeRoot.SetActive(true);
                scope.Container.Resolve<PlayerMovementModel>().SetInitialPitch(pitch);
                scope.Container.Resolve<PlayerMotor>().SetLook(rotation.eulerAngles.y, pitch);
                locationScene = scene;
                LocationScope = scope;
                additionalScenes.Remove(scene);
                SceneManager.SetActiveScene(scene);
                BindDoors(scene, scope);
                foreach (var root in previous.GetRootGameObjects()) root.SetActive(false);
                previousScope.Dispose();
                await SceneManager.UnloadSceneAsync(previous);
                await UnloadAdditionalScenesAsync();
            }
            finally { busy = false; }
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
                if (scene.IsValid() && scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
            additionalScenes.Clear();
        }

        private void ReleaseScopes()
        {
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
