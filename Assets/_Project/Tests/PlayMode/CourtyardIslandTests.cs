using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay;
using Memories.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class CourtyardIslandTests
    {
        [UnityTest]
        public IEnumerator CourtyardHasWalkableArchAndMovingCurtains() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Courtyard test");
            root.SetActive(false);
            root.AddComponent<TestLocationLauncher>().SelectedLocation = TestLocationLauncher.Location.Courtyard;
            var app = root.AddComponent<AppLifetimeScope>();
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                float deadline = Time.realtimeSinceStartup + 20;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning);
                Assert.AreEqual(TestLocationLauncher.CourtyardPath, session.LocationScope.gameObject.scene.path);
                var motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                Physics.SyncTransforms();
                void Walk(float x, float z)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        var delta = new Vector3(x - motor.transform.position.x, 0, z - motor.transform.position.z);
                        if (delta.magnitude < .06f) break;
                        motor.Move(Vector3.ClampMagnitude(delta, .05f) + Vector3.down * .04f);
                    }
                    Assert.Less(Vector2.Distance(new Vector2(x, z), new Vector2(motor.transform.position.x, motor.transform.position.z)), .15f);
                    Assert.That(motor.transform.position.y, Is.InRange(-.1f, .15f));
                }
                Walk(0, 8); // Through the arch.
                Walk(0, 20);
                Walk(8, 20);
                Walk(8, 29);
                Walk(-8, 29);
                Walk(-8, 34); // View of the broken ground-floor window.
                var scene = session.LocationScope.gameObject.scene;
                var curtains = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CurtainWind>()).ToArray();
                Assert.AreEqual(2, curtains.Length);
                var mesh = curtains[0].GetComponent<MeshFilter>().mesh;
                var before = mesh.vertices;
                await UniTask.Delay(700);
                var after = mesh.vertices;
                Assert.AreEqual(before[0], after[0], "The top edge must remain attached.");
                Assert.Greater(Vector3.Distance(before[before.Length - 1], after[after.Length - 1]), .005f, "The curtain hem must move in the wind.");
                var names = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Select(t => t.name).ToArray();
                Assert.AreEqual(1, names.Count(n => n == "Единственное кольцо"));
                Assert.AreEqual(3, names.Count(n => n.StartsWith("Контейнер ")));
                Walk(0, 20);
                Walk(0, -8);
                for (int i = 0; i < 200; i++) motor.Move(new Vector3(0, -.04f, -.08f));
                Assert.Greater(motor.transform.position.z, -12, "The player must not fall off the island.");
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
