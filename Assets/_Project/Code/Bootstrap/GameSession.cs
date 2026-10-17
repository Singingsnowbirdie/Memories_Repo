using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    // Composition and scene ownership only. Gameplay depends on its own contracts.
    public sealed class GameSession : IDisposable
    {
        private readonly AppLifetimeScope applicationScope;
        private readonly ApplicationContext application;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private Scene locationScene;
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
            if (string.IsNullOrWhiteSpace(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
                throw new ArgumentException("Location must be enabled in Build Settings: " + scenePath, nameof(scenePath));
            if (SceneManager.GetSceneByPath(scenePath).isLoaded)
                throw new InvalidOperationException("Location is already loaded. Start the game from Bootstrap.");

            busy = true;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, application.Token, shutdown.Token);
            try
            {
                SessionScope = applicationScope.CreateChild<SessionLifetimeScope>(childScopeName: "SessionLifetimeScope");
                // Unity scene loading cannot be interrupted safely. Finish the operation,
                // then honour cancellation and unload the scene we own.
                await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                locationScene = SceneManager.GetSceneByPath(scenePath);
                cancellation.Token.ThrowIfCancellationRequested();

                var root = new GameObject("LocationLifetimeScope");
                root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, locationScene);
                LocationScope = root.AddComponent<LocationLifetimeScope>();
                LocationScope.parentReference.Object = SessionScope;
                root.SetActive(true);
                SceneManager.SetActiveScene(locationScene);
            }
            catch
            {
                await CleanupAsync();
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

        private async UniTask CleanupAsync()
        {
            ReleaseScopes();
            if (applicationScope != null && applicationScope.gameObject.scene.isLoaded)
                SceneManager.SetActiveScene(applicationScope.gameObject.scene);
            if (locationScene.IsValid() && locationScene.isLoaded)
                await SceneManager.UnloadSceneAsync(locationScene);
            locationScene = default;
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
            if (Application.isPlaying && locationScene.IsValid() && locationScene.isLoaded
                && SceneManager.sceneCount > 1)
                SceneManager.UnloadSceneAsync(locationScene);
            locationScene = default;
        }
    }
}
