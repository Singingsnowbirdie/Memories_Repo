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
    public sealed class TestLocationLauncherTests
    {
        [UnityTest]
        public IEnumerator HighwayStartsAloneAndHasAnEndBarrier() => Check(TestLocationLauncher.Location.Highway);

        [UnityTest]
        public IEnumerator HouseStartsAloneAndCanBeEntered() => Check(TestLocationLauncher.Location.House);

        private static IEnumerator Check(TestLocationLauncher.Location selected) => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Isolated location test");
            root.SetActive(false);
            var launcher = root.AddComponent<TestLocationLauncher>();
            launcher.SelectedLocation = selected;
            var app = root.AddComponent<AppLifetimeScope>();
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            string path = launcher.ScenePath;
            string excluded = selected == TestLocationLauncher.Location.House
                ? TestLocationLauncher.HighwayPath : TestLocationLauncher.HousePath;
            try
            {
                float deadline = Time.realtimeSinceStartup + 20;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning);
                Assert.AreEqual(path, session.LocationScope.gameObject.scene.path);
                Assert.IsFalse(SceneManager.GetSceneByPath(excluded).isLoaded);
                Assert.IsFalse(SceneManager.GetSceneByPath(AppLifetimeScope.DefaultLocationPath).isLoaded);
                Assert.IsEmpty(app.AdditionalLocationScenes);
                var motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                Assert.AreEqual(1, motor.GetComponentsInChildren<Camera>().Length);
                Assert.AreEqual(1, motor.GetComponentsInChildren<AudioListener>().Length);
                Physics.SyncTransforms();
                if (selected == TestLocationLauncher.Location.Highway)
                {
                    Assert.That(motor.transform.position.z, Is.EqualTo(100.5f).Within(.2f));
                    for (int i = 0; i < 4000; i++) motor.Move(new Vector3(0, -.04f, .08f));
                    Assert.That(motor.transform.position.z, Is.InRange(299f, 300f));
                }
                else
                {
                    Assert.That(motor.transform.position.z, Is.EqualTo(305f).Within(.2f));
                    for (int i = 0; i < 100; i++) motor.Move(new Vector3(0, -.04f, .08f));
                    Assert.Greater(motor.transform.position.z, 312, "The entrance must be walkable.");
                    for (int i = 0; i < 400; i++) motor.Move(new Vector3(0, -.04f, -.08f));
                    Assert.That(motor.transform.position.z, Is.InRange(300f, 301f), "The isolated path needs an end barrier.");
                }
                Assert.That(motor.transform.position.y, Is.InRange(-.1f, .15f));
                await session.EndAsync();
                Assert.IsFalse(SceneManager.GetSceneByPath(path).isLoaded);
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                Object.Destroy(root);
                await UniTask.NextFrame();
            }
        });
    }
}
