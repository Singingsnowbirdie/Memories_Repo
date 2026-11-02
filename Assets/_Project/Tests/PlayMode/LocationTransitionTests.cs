using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Triggers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class LocationTransitionTests
    {
        private const string Highway = "Assets/_Project/Locations/Highway/Highway.unity";

        [UnityTest]
        public IEnumerator LocationChangeKeepsPlayerCameraAndMovementInBootstrap() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Persistent player test app");
            root.SetActive(false);
            var app = root.AddComponent<AppLifetimeScope>();
            app.StartAutomatically = false;
#if UNITY_EDITOR
            app.UseOpenLocationForEditorPlay = true;
#endif
            root.SetActive(true);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                await session.StartAsync(Highway);
                var scope = session.LocationScope;
                var motor = scope.Container.Resolve<PlayerMotor>();
                var camera = motor.GetComponentInChildren<Camera>();
                var movement = scope.Container.Resolve<PlayerMovementModel>();
                scope.Container.Resolve<PlayerPresenter>().ControlsEnabled = false;
                await UniTask.NextFrame();
                movement.SetInitialPitch(23);
                var position = motor.transform.position;
                await session.ChangeLocationAsync(TestLocationLauncher.BackyardPath);
                Assert.AreSame(scope, session.LocationScope);
                Assert.AreSame(motor, scope.Container.Resolve<PlayerMotor>());
                Assert.AreSame(camera, motor.GetComponentInChildren<Camera>());
                Assert.AreSame(movement, scope.Container.Resolve<PlayerMovementModel>());
                Assert.AreEqual(root.scene, motor.gameObject.scene);
                Assert.AreEqual(root.scene, camera.gameObject.scene);
                Assert.AreEqual(23, movement.Pitch);
                Assert.Less(Vector3.Distance(position, motor.transform.position), .1f);
                Assert.IsFalse(SceneManager.GetSceneByPath(Highway).isLoaded);
            }
            finally
            {
                if (session.IsRunning) await session.EndAsync();
                UnityEngine.Object.Destroy(root);
                await UniTask.NextFrame();
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
                await session.StartAsync(Highway);
                var original = session.LocationScope;
                bool rejected = false;
                try { await session.ChangeLocationAsync("Assets/Scenes/MissingLocation.unity"); }
                catch (InvalidOperationException) { rejected = true; }
                Assert.IsTrue(rejected);
                Assert.AreSame(original, session.LocationScope);
                Assert.IsTrue(SceneManager.GetSceneByPath(Highway).isLoaded);
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
                    typeof(ReactionNode).GetField("locationScenePath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, Highway);
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
