using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using Memories.Gameplay.Triggers;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Interaction;
using Memories.Contracts;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using XNode;

namespace Memories.Tests.PlayMode
{
    public sealed class ReactionGraphTests
    {
        private readonly List<Object> owned = new List<Object>();
        private ReactionGraph graph;
        private StoryTextGraph textGraph;
        private MemoryGraph memoryGraph;
        private ReactionStartNode start;
        private ReactionNode first, memory, last;
        private ReactionEndNode end;

        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Connect(Node from, Node to) => from.GetOutputPort("next").Connect(to.GetInputPort("previous"));

        [SetUp]
        public void CreateGraphs()
        {
            textGraph = Own(ScriptableObject.CreateInstance<StoryTextGraph>());
            var ts = Own(textGraph.AddNode<StoryTextStartNode>());
            var text = Own(textGraph.AddNode<StoryTextNode>());
            Set(text, "text", "Trigger graph test"); Set(text, "duration", .02f);
            var te = Own(textGraph.AddNode<StoryTextEndNode>());
            Connect(ts, text); Connect(text, te);
            memoryGraph = Own(ScriptableObject.CreateInstance<MemoryGraph>());
            var ms = Own(memoryGraph.AddNode<MemoryStartNode>());
            var frame = Own(memoryGraph.AddNode<MemoryFrameNode>());
            Set(frame, "image", Texture2D.whiteTexture);
            Set(frame, "transition", MemoryFrameTransition.Instant);
            Set(frame, "displayDuration", .1f);
            var me = Own(memoryGraph.AddNode<MemoryEndNode>());
            Connect(ms, frame); Connect(frame, me);
            graph = Own(ScriptableObject.CreateInstance<ReactionGraph>());
            start = Own(graph.AddNode<ReactionStartNode>());
            first = Own(graph.AddNode<ReactionNode>());
            memory = Own(graph.AddNode<ReactionNode>());
            last = Own(graph.AddNode<ReactionNode>());
            end = Own(graph.AddNode<ReactionEndNode>());
            Set(first, "textGraph", textGraph); Set(last, "textGraph", textGraph);
            Set(memory, "kind", ReactionKind.Memory);
            Set(memory, "memoryGraph", memoryGraph); Set(memory, "memoryId", "graph-test-memory");
            Connect(start, first); Connect(first, memory); Connect(memory, last); Connect(last, end);
        }

        [Test]
        public void ValidatesWholeChainAndReferencedGraphs()
        {
            Assert.IsTrue(graph.TryGetSequence(out var steps, out var error), error);
            CollectionAssert.AreEqual(new[] { first, memory, last }, steps);
            Set(first, "textGraph", null);
            Assert.IsFalse(graph.TryGetSequence(out _, out _));
            Set(first, "textGraph", textGraph);
            last.GetOutputPort("next").ClearConnections();
            Connect(last, first);
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Cycles must be rejected.");
            last.GetOutputPort("next").ClearConnections();
            Connect(start, first); Connect(last, end);
            var disconnected = Own(graph.AddNode<ReactionNode>());
            Set(disconnected, "textGraph", textGraph);
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Disconnected actions must be rejected.");
        }

        [Test]
        public void InventoryConditionSelectsBranchAndAllowsSharedContinuation()
        {
            var item = Own(ScriptableObject.CreateInstance<QuestItem>());
            Set(item, "id", "remote");
            var condition = Own(graph.AddNode<InventoryConditionNode>());
            Set(condition, "item", item);
            start.GetOutputPort("next").ClearConnections();
            first.GetOutputPort("next").ClearConnections();
            Connect(start, condition);
            condition.GetOutputPort("ifPresent").Connect(first.GetInputPort("previous"));
            condition.GetOutputPort("ifAbsent").Connect(memory.GetInputPort("previous"));
            Connect(first, last);
            Assert.IsTrue(graph.TryGetSequence(out var actions, out var error), error);
            Assert.AreEqual(3, actions.Count, "Shared continuation must only be validated once.");
            var inventory = new QuestInventory();
            Assert.AreEqual("ifAbsent", condition.SelectOutput(inventory));
            inventory.TryAdd(item);
            Assert.AreEqual("ifPresent", condition.SelectOutput(inventory));
            Assert.IsTrue(inventory.Contains(item.Id), "Checking must not consume the item.");
            condition.GetOutputPort("ifAbsent").ClearConnections();
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Both branches must be connected.");
            condition.GetOutputPort("ifAbsent").Connect(condition.GetInputPort("previous"));
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "Branch cycles must be rejected.");
            condition.GetOutputPort("ifAbsent").ClearConnections();
            condition.GetOutputPort("ifAbsent").Connect(memory.GetInputPort("previous"));
            Set(condition, "item", null);
            Assert.IsFalse(graph.TryGetSequence(out _, out _), "A missing item must be rejected.");
        }

        [UnityTest]
        public IEnumerator RunsInOrderRepeatsAndKeepsStatePerTrigger() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Scenes/Bootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            GameSession session = null;
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.TryGetComponent<AppLifetimeScope>(out var app)) session = app.Container.Resolve<GameSession>();
                Assert.IsNotNull(session);
                await UniTask.WaitUntil(() => session.IsRunning);
                var texts = session.LocationScope.Container.Resolve<StoryTextPresenter>();
                var memories = session.LocationScope.Container.Resolve<MemoryPresenter>();
                var trigger = CreateTrigger(texts, memories);
                Assert.IsTrue(trigger.TryActivate());
                Assert.IsFalse(trigger.TryActivate(), "No overlapping activation.");
                Assert.IsTrue(texts.IsPlaying);
                Assert.IsFalse(memories.IsPlaying, "Memory must wait for text completion.");
                await WaitUntil(() => memories.IsPlaying);
                Assert.IsFalse(texts.IsPlaying);
                await WaitUntil(() => !memories.IsPlaying);
                await WaitUntil(() => texts.IsPlaying);
                await WaitUntil(() => !trigger.IsRunning);
                Assert.IsFalse(trigger.TryActivate(), "One-shot state is stored on the scene trigger.");
                var other = CreateTrigger(texts, memories);
                Assert.IsTrue(other.TryActivate(), "Sharing an asset must not share fired state.");
                await WaitUntil(() => !other.IsRunning);
                Set(start, "runOnce", false);
                Assert.IsTrue(trigger.TryActivate(), "Repeat also replays already collected memories.");
                await WaitUntil(() => memories.IsPlaying);
                trigger.gameObject.SetActive(false);
                await WaitUntil(() => !trigger.IsRunning);
                Assert.IsFalse(memories.IsPlaying);
                Assert.AreEqual(1, Time.timeScale);
                Set(start, "runOnce", true);
                var objectRoot = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
                objectRoot.transform.position = Vector3.one * 600;
                var interactable = objectRoot.AddComponent<ReactionInteractable>();
                Set(interactable, "graph", graph);
                Set(interactable, "prompt", "Прочитать");
                interactable.Initialize(texts, memories);
                var cameraRoot = Own(new GameObject("Interaction test camera"));
                var camera = cameraRoot.AddComponent<Camera>();
                camera.transform.position = objectRoot.transform.position - Vector3.forward * 2;
                Physics.SyncTransforms();
                var probe = new InteractionProbe(camera);
                var input = new TestInput();
                var view = new TestView();
                var interaction = new InteractionPresenter(input, probe, view);
                interaction.Tick();
                Assert.AreSame(interactable, probe.FindTarget());
                Assert.AreEqual("[E] Прочитать", view.Prompt);
                input.Pressed = true;
                interaction.Tick();
                input.Pressed = false;
                Assert.IsTrue(interactable.IsRunning);
                Assert.IsFalse(interactable.CanInteract);
                Assert.IsTrue(objectRoot.GetComponent<Renderer>().enabled, "Interactive objects stay visible.");
                await WaitUntil(() => !interactable.IsRunning);
                interaction.Tick();
                Assert.IsFalse(view.Available, "One-shot interaction no longer offers a prompt.");
                Set(start, "runOnce", false);
                interaction.Tick();
                Assert.IsTrue(view.Available);
                input.Pressed = true;
                interaction.Tick();
                Assert.IsTrue(interactable.IsRunning);
                objectRoot.SetActive(false);
                await WaitUntil(() => !interactable.IsRunning);
                Assert.IsFalse(texts.IsPlaying);
            }
            finally
            {
                if (session != null && session.IsRunning) await session.EndAsync();
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
            }
        });

        private sealed class TestInput : IInteractionInput
        {
            public bool Pressed;
            public bool InteractionEnabled => true;
            public bool InteractPressed => Pressed;
        }
        private sealed class TestView : IInteractionView
        {
            public string Prompt;
            public bool Available;
            public void Show(bool visible, bool available, string prompt) { Prompt = prompt; Available = available; }
        }

        private SceneTrigger CreateTrigger(StoryTextPresenter texts, MemoryPresenter memories)
        {
            var root = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            root.transform.position = Vector3.one * 500;
            root.GetComponent<Collider>().isTrigger = true;
            var trigger = root.AddComponent<SceneTrigger>();
            Set(trigger, "graph", graph);
            trigger.Initialize(texts, memories);
            return trigger;
        }

        private static async UniTask WaitUntil(System.Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!condition() && Time.realtimeSinceStartup < deadline) await UniTask.NextFrame();
            Assert.IsTrue(condition(), "Timed out waiting for trigger playback.");
        }

        [TearDown]
        public void DestroyOwned()
        {
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }
    }
}
