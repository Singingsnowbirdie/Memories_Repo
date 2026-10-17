using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.EditMode
{
    public sealed class MemoryPresenterTests
    {
        private sealed class Input : IPauseInput
        {
            public event Action PauseRequested;
            public bool Blocked;
            public void SetPaused(bool paused) => Blocked = paused;
            public void Escape() => PauseRequested?.Invoke();
        }
        private sealed class View : IMemoryView
        {
            public bool Visible;
            public void Show() => Visible = true;
            public void Hide() => Visible = false;
            public UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token)
                => UniTask.WaitUntilCanceled(token);
        }
        private sealed class Music : IMemoryMusic
        {
            public int Executed, Stopped;
            public void Execute(MemoryMusicNode node) => Executed++;
            public void StopImmediately() => Stopped++;
        }

        [Test]
        public void GraphRejectsMissingImageBrokenConnectionAndCycle()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MemoryGraph>("Assets/_Project/Configs/FirstMemoryGraph.asset");
            Assert.IsTrue(asset.TryGetSequence(out var frames, out var error), error);
            Assert.AreEqual(3, frames.OfType<MemoryFrameNode>().Count());
            Assert.IsInstanceOf<MemoryMusicNode>(frames[0]);
            var graph = (MemoryGraph)asset.Copy();
            try
            {
                var frame = graph.nodes.OfType<MemoryFrameNode>().First();
                var data = new SerializedObject(frame);
                var image = frame.Image;
                data.FindProperty("image").objectReferenceValue = null;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
                data.Update();
                data.FindProperty("image").objectReferenceValue = image;
                data.ApplyModifiedPropertiesWithoutUndo();
                frame.GetOutputPort("next").ClearConnections();
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
                frame.GetOutputPort("next").Connect(frame.GetInputPort("previous"));
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
            }
            finally { DestroyGraph(graph); }
        }

        [Test]
        public void InvalidGraphDoesNotBlockInputAndEscapeDoesNotCollect()
        {
            var input = new Input();
            var state = new GameplayModeState(input);
            var collection = new MemoryCollection();
            var view = new View();
            var graph = AssetDatabase.LoadAssetAtPath<MemoryGraph>("Assets/_Project/Configs/FirstMemoryGraph.asset");
            var invalid = ScriptableObject.CreateInstance<MemoryGraph>();
            float time = Time.timeScale;
            bool audio = AudioListener.pause;
            var music = new Music();
            using var presenter = new MemoryPresenter(collection, state, view, input, music);
            try
            {
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Воспоминание не запущено"));
                Assert.IsFalse(presenter.Open("bad", invalid));
                Assert.IsFalse(input.Blocked || view.Visible);
                Assert.IsTrue(presenter.Open("first", graph));
                Assert.AreEqual(1, music.Executed);
                Assert.IsTrue(input.Blocked && view.Visible);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsFalse(presenter.Open("second", graph));
                input.Escape();
                Assert.AreEqual(1, music.Stopped);
                Assert.IsFalse(presenter.IsPlaying || view.Visible || input.Blocked);
                Assert.IsFalse(collection.Contains("first"));
                Assert.AreEqual(time, Time.timeScale);
                Assert.IsTrue(presenter.Open("first", graph));
                presenter.Dispose();
                Assert.AreEqual(2, music.Stopped);
                Assert.AreEqual(time, Time.timeScale);
                Assert.AreEqual(audio, AudioListener.pause);
                Assert.IsFalse(collection.Contains("first"));
            }
            finally
            {
                presenter.Dispose();
                UnityEngine.Object.DestroyImmediate(invalid);
                Time.timeScale = time;
                AudioListener.pause = audio;
            }
        }

        private static void DestroyGraph(MemoryGraph graph)
        {
            foreach (var node in graph.nodes.ToArray()) UnityEngine.Object.DestroyImmediate(node);
            UnityEngine.Object.DestroyImmediate(graph);
        }
    }
}
