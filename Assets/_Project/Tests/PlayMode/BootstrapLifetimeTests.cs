using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class BootstrapLifetimeTests
    {
        private AppLifetimeScope app;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            var root = new GameObject("TestApp");
            root.SetActive(false);
            app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
            root.SetActive(true);
            session = app.Container.Resolve<GameSession>();
        }

        [UnityTearDown]
        public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
        {
            if (session != null) await session.EndAsync();
            if (app != null) UnityEngine.Object.Destroy(app.gameObject);
            await UniTask.NextFrame();
        });

        [UnityTest]
        public IEnumerator StartEndAndRestartPreserveAppAndReleaseChildren() => UniTask.ToCoroutine(async () =>
        {
            var application = app.Container.Resolve<ApplicationContext>();
            await session.StartAsync(AppLifetimeScope.DefaultLocationPath);
            var firstSession = session.SessionScope.Container.Resolve<SessionContext>();
            var firstLocation = session.LocationScope.Container.Resolve<LocationContext>();
            Assert.AreSame(app, session.SessionScope.Parent);
            Assert.AreSame(session.SessionScope, session.LocationScope.Parent);
            Assert.AreSame(application, session.LocationScope.Container.Resolve<ApplicationContext>());
            Assert.AreSame(firstSession, session.LocationScope.Container.Resolve<SessionContext>());
            Assert.AreEqual(AppLifetimeScope.DefaultLocationPath, session.LocationScope.gameObject.scene.path);

            await session.EndAsync();
            Assert.IsTrue(firstLocation.Token.IsCancellationRequested);
            Assert.IsTrue(firstSession.Token.IsCancellationRequested);
            Assert.IsFalse(application.Token.IsCancellationRequested);
            Assert.IsFalse(SceneManager.GetSceneByPath(AppLifetimeScope.DefaultLocationPath).isLoaded);

            await session.StartAsync(AppLifetimeScope.DefaultLocationPath);
            Assert.AreNotEqual(firstSession.Id, session.SessionScope.Container.Resolve<SessionContext>().Id);
            Assert.AreNotEqual(firstLocation.Id, session.LocationScope.Container.Resolve<LocationContext>().Id);
            Assert.AreSame(application, session.LocationScope.Container.Resolve<ApplicationContext>());
        });

        [UnityTest]
        public IEnumerator CancelledLoadLeavesNoSessionOrLocation() => UniTask.ToCoroutine(async () =>
        {
            using var cancellation = new CancellationTokenSource();
            var start = session.StartAsync(AppLifetimeScope.DefaultLocationPath, cancellation.Token);
            cancellation.Cancel();
            bool cancelled = false;
            try { await start; }
            catch (OperationCanceledException) { cancelled = true; }
            Assert.IsTrue(cancelled);
            Assert.IsNull(session.SessionScope);
            Assert.IsNull(session.LocationScope);
            Assert.IsFalse(SceneManager.GetSceneByPath(AppLifetimeScope.DefaultLocationPath).isLoaded);
        });

        [UnityTest]
        public IEnumerator DuplicateStartIsRejectedWithoutReplacingSession() => UniTask.ToCoroutine(async () =>
        {
            await session.StartAsync(AppLifetimeScope.DefaultLocationPath);
            var original = session.SessionScope;
            bool rejected = false;
            try { await session.StartAsync(AppLifetimeScope.DefaultLocationPath); }
            catch (InvalidOperationException) { rejected = true; }
            Assert.IsTrue(rejected);
            Assert.AreSame(original, session.SessionScope);
        });

        [UnityTest]
        public IEnumerator MissingSceneDoesNotCreateSession() => UniTask.ToCoroutine(async () =>
        {
            bool rejected = false;
            try { await session.StartAsync("Assets/Scenes/DoesNotExist.unity"); }
            catch (ArgumentException) { rejected = true; }
            Assert.IsTrue(rejected);
            Assert.IsNull(session.SessionScope);
        });
    }
}
