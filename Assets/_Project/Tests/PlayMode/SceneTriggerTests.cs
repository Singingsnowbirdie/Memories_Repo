using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.Story;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using Memories.Gameplay.Triggers;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class CountingTriggerEvent : TriggerEvent
    {
        public int Count;
        public override void Initialize(StoryTextPresenter texts, MemoryPresenter memories) { }
        public override bool TryExecute() { Count++; return true; }
    }

    public sealed class SceneTriggerTests
    {
        private GameObject zone, actor;

        [UnityTest]
        public IEnumerator EventsUseConfiguredGraphs() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Locations/ForestIsland/ForestIslandBootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            GameSession session = null;
            var textGraph = ScriptableObject.CreateInstance<StoryTextGraph>();
            var textStart = textGraph.AddNode<StoryTextStartNode>();
            var text = textGraph.AddNode<StoryTextNode>();
            JsonUtility.FromJsonOverwrite("{\"text\":\"Trigger text\",\"duration\":3}", text);
            var textEnd = textGraph.AddNode<StoryTextEndNode>();
            textStart.GetOutputPort("next").Connect(text.GetInputPort("previous"));
            text.GetOutputPort("next").Connect(textEnd.GetInputPort("previous"));
            var memoryGraph = ScriptableObject.CreateInstance<MemoryGraph>();
            var memoryStart = memoryGraph.AddNode<MemoryStartNode>();
            var frame = memoryGraph.AddNode<MemoryFrameNode>();
            Set(frame, "image", Texture2D.whiteTexture);
            var memoryEnd = memoryGraph.AddNode<MemoryEndNode>();
            memoryStart.GetOutputPort("next").Connect(frame.GetInputPort("previous"));
            frame.GetOutputPort("next").Connect(memoryEnd.GetInputPort("previous"));
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.TryGetComponent<AppLifetimeScope>(out var app)) session = app.Container.Resolve<GameSession>();
                Assert.IsNotNull(session);
                await UniTask.WaitUntil(() => session.IsRunning);
                var texts = session.LocationScope.Container.Resolve<StoryTextPresenter>();
                var memories = session.LocationScope.Container.Resolve<MemoryPresenter>();
                zone = new GameObject("Trigger events");
                var textEvent = zone.AddComponent<ShowStoryTextEvent>();
                var memoryEvent = zone.AddComponent<ShowMemoryEvent>();
                Set(textEvent, "graph", textGraph);
                Set(memoryEvent, "graph", memoryGraph);
                Set(memoryEvent, "memoryId", "trigger-test");
                textEvent.Initialize(texts, memories);
                memoryEvent.Initialize(texts, memories);
                Assert.IsTrue(textEvent.TryExecute());
                await UniTask.Delay(400);
                Assert.IsTrue(texts.IsPlaying);
                Assert.IsTrue(session.LocationScope.GetComponentInChildren<StoryTextView>().GetComponent<Canvas>().enabled);
                texts.Stop();
                Assert.IsTrue(memoryEvent.TryExecute());
                Assert.IsTrue(memories.IsPlaying);
                Assert.IsFalse(memoryEvent.TryExecute(), "Cannot open a second memory while reading.");
                memories.Close();
                Assert.IsFalse(memories.IsPlaying);
            }
            finally
            {
                if (session != null && session.IsRunning) await session.EndAsync();
                if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene);
                foreach (var node in textGraph.nodes) Object.DestroyImmediate(node);
                foreach (var node in memoryGraph.nodes) Object.DestroyImmediate(node);
                Object.DestroyImmediate(textGraph);
                Object.DestroyImmediate(memoryGraph);
            }
        });

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTest]
        public IEnumerator PhysicsFiltersPlayerHidesPrimitiveAndFiresOnce()
        {
            zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.transform.position = new Vector3(500, 500, 500);
            zone.GetComponent<Collider>().isTrigger = true;
            var action = zone.AddComponent<CountingTriggerEvent>();
            zone.AddComponent<SceneTrigger>();
            Assert.IsFalse(zone.GetComponent<Renderer>().enabled);
            Assert.IsTrue(zone.GetComponent<Rigidbody>().isKinematic);
            actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            actor.transform.position = zone.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, action.Count, "Other scene objects must not trigger events.");
            Object.Destroy(actor);
            yield return null;
            actor = new GameObject("Player", typeof(CharacterController), typeof(PlayerMotor));
            actor.transform.position = zone.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, action.Count);
            actor.transform.position += Vector3.right * 10;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            actor.transform.position = zone.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, action.Count);
            JsonUtility.FromJsonOverwrite("{\"triggerOnce\":false}", zone.GetComponent<SceneTrigger>());
            actor.transform.position += Vector3.right * 10;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            actor.transform.position = zone.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(2, action.Count);
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(zone);
        }
    }
}
