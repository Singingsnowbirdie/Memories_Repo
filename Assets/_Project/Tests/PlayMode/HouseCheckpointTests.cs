using System.Collections;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class HouseCheckpointTests
    {
        [UnityTest]
        public IEnumerator HouseResumeCreatesPlayerWithoutLoadingHighway() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("House checkpoint test app");
            root.SetActive(false);
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
            GameSession session = null;
            try
            {
                root.SetActive(true);
                session = app.Container.Resolve<GameSession>();
                await session.StartAsync(TestLocationLauncher.HousePath, resumeHouse: true);
                await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning);
                Assert.AreEqual(TestLocationLauncher.HousePath, session.LocationScope.gameObject.scene.path);
                Assert.IsFalse(SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath).isLoaded);
                var player = session.LocationScope.Container.Resolve<PlayerMotor>();
                Assert.That(player.transform.position.z, Is.EqualTo(305).Within(.1f));
                Assert.IsNotNull(player.GetComponentInChildren<Camera>());
                player.Teleport(new Vector3(0, 1, 312), 90);
                Assert.IsTrue(session.ReturnToLocationStart());
                Assert.That(player.transform.position.z, Is.EqualTo(305).Within(.01f));
            }
            finally
            {
                if (session != null && session.IsRunning) await session.EndAsync();
                Object.Destroy(root);
            }
        });
    }
}
