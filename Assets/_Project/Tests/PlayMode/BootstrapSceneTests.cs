using System.Collections;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class BootstrapSceneTests
    {
        [UnityTest]
        public IEnumerator BootstrapSceneStartsLocationAutomatically() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Scenes/Bootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            AppLifetimeScope app = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<AppLifetimeScope>(out var candidate)) app = candidate;
            try
            {
                Assert.IsNotNull(app);
                var session = app.Container.Resolve<GameSession>();
                float deadline = Time.realtimeSinceStartup + 20;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning, "Bootstrap did not start the initial location.");
                var application = app.Container.Resolve<ApplicationContext>();
                var sessionContext = session.SessionScope.Container.Resolve<SessionContext>();
                var location = session.LocationScope.Container.Resolve<LocationContext>();
                await session.EndAsync();
                await SceneManager.UnloadSceneAsync(scene);
                Assert.IsTrue(application.Token.IsCancellationRequested);
                Assert.IsTrue(sessionContext.Token.IsCancellationRequested);
                Assert.IsTrue(location.Token.IsCancellationRequested);
            }
            finally
            {
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
            }
        });
    }
}
