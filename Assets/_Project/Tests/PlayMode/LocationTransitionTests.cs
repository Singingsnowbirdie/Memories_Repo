using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Triggers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class LocationTransitionTests
    {
        private const string Airport = "Assets/_Project/Locations/AirportIsland/AirportIsland.unity";
        private const string Forest = "Assets/_Project/Locations/ForestIsland/ForestIsland.unity";

        [UnityTest]
        public IEnumerator MainBootstrapExitLoadsAirportAndPreservesSession() => UniTask.ToCoroutine(async () =>
        {
            const string bootstrapPath = "Assets/_Project/Scenes/Bootstrap.unity";
            await SceneManager.LoadSceneAsync(bootstrapPath, LoadSceneMode.Additive);
            var bootstrap = SceneManager.GetSceneByPath(bootstrapPath);
            var app = bootstrap.GetRootGameObjects().Select(g => g.GetComponent<AppLifetimeScope>()).First(a => a != null);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                float deadline = Time.realtimeSinceStartup + 30;
                while (!session.IsRunning && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
                Assert.IsTrue(session.IsRunning);
                Assert.AreEqual(Forest, session.LocationScope.gameObject.scene.path);
                var sessionContext = session.SessionScope.Container.Resolve<SessionContext>();
                var oldLocation = session.LocationScope.Container.Resolve<LocationContext>();
                var collection = session.SessionScope.Container.Resolve<MemoryCollection>();
                collection.Collect("forest-transition-test");
                var exit = GameObject.Find("Forest exit - Airport").GetComponent<SceneTrigger>();
                Assert.IsNotNull(exit);
                Assert.IsFalse(exit.GetComponent<Renderer>().enabled);
                var motor = session.LocationScope.Container.Resolve<PlayerMotor>();
                var controller = motor.GetComponent<CharacterController>();
                controller.enabled = false;
                motor.transform.position = new Vector3(0, .12f, 70.5f);
                controller.enabled = true;
                Physics.SyncTransforms();
                for (int i = 0; i < 50 && exit != null && motor != null && !exit.IsRunning; i++)
                {
                    motor.Move(new Vector3(0, -.02f, .08f));
                    await UniTask.WaitForFixedUpdate();
                }
                deadline = Time.realtimeSinceStartup + 30;
                while (Time.realtimeSinceStartup < deadline &&
                    (SceneManager.GetSceneByPath(Forest).isLoaded || !session.IsRunning ||
                     session.LocationScope.gameObject.scene.path != Airport ||
                     !session.CanChangeLocation(Forest))) await UniTask.NextFrame();
                Assert.AreEqual(Airport, session.LocationScope.gameObject.scene.path);
                Assert.IsFalse(SceneManager.GetSceneByPath(Forest).isLoaded);
                Assert.AreSame(sessionContext, session.SessionScope.Container.Resolve<SessionContext>());
                Assert.IsFalse(sessionContext.IsDisposed);
                Assert.IsTrue(oldLocation.IsDisposed);
                Assert.AreSame(collection, session.SessionScope.Container.Resolve<MemoryCollection>());
                Assert.IsTrue(collection.Contains("forest-transition-test"));
                var airportMotor = session.LocationScope.Container.Resolve<PlayerMotor>();
                Assert.That(airportMotor.transform.position.x, Is.EqualTo(-27).Within(.2f));
                Assert.That(airportMotor.transform.position.z, Is.EqualTo(-62).Within(.2f));
                Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length);
                Assert.IsFalse(app.Container.Resolve<ILocationTransition>().CanChangeLocation(Airport));
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                if (bootstrap.isLoaded) await SceneManager.UnloadSceneAsync(bootstrap);
            }
        });

        [UnityTest]
        public IEnumerator InvalidDestinationLeavesCurrentLocationIntact() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Transition test app");
            root.SetActive(false);
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                await session.StartAsync(Forest);
                var original = session.LocationScope;
                bool rejected = false;
                try { await session.ChangeLocationAsync("Assets/Scenes/MissingAirport.unity"); }
                catch (InvalidOperationException) { rejected = true; }
                Assert.IsTrue(rejected);
                Assert.AreSame(original, session.LocationScope);
                Assert.IsTrue(SceneManager.GetSceneByPath(Forest).isLoaded);
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                UnityEngine.Object.Destroy(root);
                await UniTask.NextFrame();
            }
        });

        [Test]
        public void LocationTransitionMustEndReactionSequence()
        {
            var graph = ScriptableObject.CreateInstance<ReactionGraph>();
            try
            {
                var start = graph.AddNode<ReactionStartNode>();
                var first = graph.AddNode<ReactionNode>();
                var second = graph.AddNode<ReactionNode>();
                var end = graph.AddNode<ReactionEndNode>();
                foreach (var node in new[] { first, second })
                {
                    typeof(ReactionNode).GetField("kind", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, ReactionKind.ChangeLocation);
                    typeof(ReactionNode).GetField("locationScenePath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, Airport);
                }
                start.GetOutputPort("next").Connect(first.GetInputPort("previous"));
                first.GetOutputPort("next").Connect(second.GetInputPort("previous"));
                second.GetOutputPort("next").Connect(end.GetInputPort("previous"));
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
                graph.RemoveNode(second);
                UnityEngine.Object.DestroyImmediate(second);
                first.GetOutputPort("next").Connect(end.GetInputPort("previous"));
                Assert.IsTrue(graph.TryGetSequence(out _, out var error), error);
            }
            finally
            {
                foreach (var node in graph.nodes.ToArray()) UnityEngine.Object.DestroyImmediate(node);
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }
    }
}
