using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Memories.Tests.PlayMode
{
    public sealed class MemoryLibraryViewTests
    {
        private sealed class Input : IPauseInput
        {
            public event Action PauseRequested;
            public bool Blocked;
            public void SetPaused(bool value) => Blocked = value;
            public void Escape() => PauseRequested?.Invoke();
        }
        private sealed class View : IMemoryView
        {
            public void Show() { }
            public void Hide() { }
            public UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token) => UniTask.CompletedTask;
        }
        private sealed class Music : IMemoryMusic
        {
            public void Execute(MemoryMusicNode node) { }
            public void StopImmediately() { }
        }
        private sealed class Sounds : IMemorySound
        {
            public UniTask PlayAsync(MemorySoundNode node, CancellationToken token) => UniTask.CompletedTask;
            public void StopAll() { }
        }
        private static Button Button(PauseMenuView view, string caption) => view.GetComponentsInChildren<Button>()
            .Single(button => button.GetComponentInChildren<Text>()?.text == caption);

        [UnityTest]
        public IEnumerator CompletionCollectsAndLibraryPaginatesAndReturnsFromReplayWhilePaused()
        {
            var root = new GameObject("Memory library test", typeof(RectTransform));
            var graph = ScriptableObject.CreateInstance<MemoryGraph>();
            var texture = new Texture2D(4, 3);
            var start = graph.AddNode<MemoryStartNode>();
            var frame = graph.AddNode<MemoryFrameNode>();
            var end = graph.AddNode<MemoryEndNode>();
            typeof(MemoryFrameNode).GetField("image", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(frame, texture);
            typeof(MemoryFrameNode).GetField("displayDuration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(frame, .02f);
            start.GetOutputPort("next").Connect(frame.GetInputPort("previous"));
            frame.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            graph.name = "Тестовое воспоминание";
            var input = new Input();
            var state = new GameplayModeState(input);
            var collection = new MemoryCollection();
            var view = root.AddComponent<PauseMenuView>();
            var pause = new PausePresenter(input, view, state, new AudioVolumeSettings());
            var memories = new MemoryPresenter(collection, state, new View(), input, new Music(), new Sounds());
            pause.ConnectMemories(collection, memories);
            float oldTime = Time.timeScale;
            bool oldAudio = AudioListener.pause;
            try
            {
                input.Escape();
                Button(view, "Копилка воспоминаний").onClick.Invoke();
                Assert.IsTrue(view.GetComponentsInChildren<Text>().Any(t => t.text == "Пока нет найденных воспоминаний"));
                Button(view, "Назад").onClick.Invoke();
                pause.Resume();
                Assert.IsTrue(memories.Open("found", graph));
                yield return new WaitForSecondsRealtime(.15f);
                Assert.IsFalse(memories.IsPlaying);
                Assert.IsTrue(collection.Contains("found"));
                Assert.AreSame(graph, collection.Memories.Single().Graph);
                for (int i = 0; i < 5; i++) collection.Collect("extra-" + i, graph);
                input.Escape();
                Button(view, "Копилка воспоминаний").onClick.Invoke();
                Assert.AreEqual(4, view.GetComponentsInChildren<RawImage>().Length);
                Button(view, "→").onClick.Invoke();
                Assert.AreEqual(2, view.GetComponentsInChildren<RawImage>().Length);
                Assert.IsFalse(Button(view, "→").interactable);
                var card = view.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<RawImage>() != null);
                card.onClick.Invoke();
                Assert.AreEqual(GameplayMode.Reading, state.Mode);
                Assert.IsEmpty(view.GetComponentsInChildren<Button>());
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(GameplayMode.Paused, state.Mode);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsTrue(input.Blocked);
                Assert.AreEqual(6, collection.Memories.Count);
                Assert.AreEqual(2, view.GetComponentsInChildren<RawImage>().Length);
                input.Escape();
                Assert.IsNotNull(Button(view, "Продолжить"));
                input.Escape();
                Assert.AreEqual(GameplayMode.Playing, state.Mode);
            }
            finally
            {
                memories.Dispose(); pause.Dispose();
                UnityEngine.Object.Destroy(root);
                foreach (var node in graph.nodes.ToArray()) UnityEngine.Object.Destroy(node);
                UnityEngine.Object.Destroy(graph);
                UnityEngine.Object.Destroy(texture);
                Time.timeScale = oldTime; AudioListener.pause = oldAudio;
            }
        }
    }
}
