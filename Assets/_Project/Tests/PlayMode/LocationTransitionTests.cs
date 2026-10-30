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
