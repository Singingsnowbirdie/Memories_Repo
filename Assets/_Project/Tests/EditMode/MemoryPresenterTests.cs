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
        private sealed class Input : IPauseInput, IAdvanceInput
        {
            public event Action PauseRequested;
            public event Action AdvanceRequested;
            public void Advance() => AdvanceRequested?.Invoke();
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

        private sealed class AdvancingView : IMemoryView
        {
            public int Frames;
            public void Show() { }
            public void Hide() { }
            public UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token)
            { Frames++; return UniTask.CompletedTask; }
        }

        [UnityTest]
        public System.Collections.IEnumerator AdvanceMovesOneFrameAndCompletesLastFrame()
        {
            var graph = ScriptableObject.CreateInstance<MemoryGraph>();
            var start = graph.AddNode<MemoryStartNode>();
            var first = graph.AddNode<MemoryFrameNode>();
            var second = graph.AddNode<MemoryFrameNode>();
            var end = graph.AddNode<MemoryEndNode>();
            foreach (var frame in new[] { first, second })
            {
                var data = new SerializedObject(frame);
                data.FindProperty("image").objectReferenceValue = Texture2D.whiteTexture;
                data.FindProperty("displayDuration").floatValue = 60;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            start.GetOutputPort("next").Connect(first.GetInputPort("previous"));
            first.GetOutputPort("next").Connect(second.GetInputPort("previous"));
            second.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            var input = new Input();
            var view = new AdvancingView();
            var collection = new MemoryCollection(persistProgress: false);
            var presenter = new MemoryPresenter(collection, new GameplayModeState(input), view, input, new Music(), new Sounds());
            try
            {
                Assert.IsTrue(presenter.Open("advance-test", graph));
                Assert.AreEqual(1, view.Frames);
                input.Advance();
                yield return null;
                yield return null;
                Assert.AreEqual(2, view.Frames);
                Assert.IsTrue(presenter.IsPlaying);
                yield return null;
                Assert.IsTrue(presenter.IsPlaying);
                input.Advance();
                yield return null;
                yield return null;
                Assert.IsFalse(presenter.IsPlaying);
                Assert.IsTrue(collection.Contains("advance-test"));
                Assert.IsFalse(input.Blocked);
            }
            finally { presenter.Dispose(); DestroyGraph(graph); }
        }

        private sealed class Sounds : IMemorySound
        {
            public UniTask PlayAsync(MemorySoundNode node, CancellationToken token) => UniTask.CompletedTask;
            public void StopAll() { }
        }

        [Test]
        public void GraphRejectsMissingImageBrokenConnectionAndCycle()
        {
            var graph = ScriptableObject.CreateInstance<MemoryGraph>();
            var start = graph.AddNode<MemoryStartNode>();
            var sample = graph.AddNode<MemoryFrameNode>();
            var end = graph.AddNode<MemoryEndNode>();
            var sampleData = new SerializedObject(sample);
            sampleData.FindProperty("image").objectReferenceValue = Texture2D.whiteTexture;
            sampleData.ApplyModifiedPropertiesWithoutUndo();
            start.GetOutputPort("next").Connect(sample.GetInputPort("previous"));
            sample.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            Assert.IsTrue(graph.TryGetSequence(out _, out var error), error);
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
        public void FirstViewingIgnoresEscapeButCollectedReplayCanClose()
        {
            var input = new Input();
            var state = new GameplayModeState(input);
            var collection = new MemoryCollection();
            var view = new View();
            var graph = ScriptableObject.CreateInstance<MemoryGraph>();
            var start = graph.AddNode<MemoryStartNode>();
            var musicNode = graph.AddNode<MemoryMusicNode>();
            var frame = graph.AddNode<MemoryFrameNode>();
            var frameData = new SerializedObject(frame);
            frameData.FindProperty("image").objectReferenceValue = Texture2D.whiteTexture;
            frameData.ApplyModifiedPropertiesWithoutUndo();
            var end = graph.AddNode<MemoryEndNode>();
            start.GetOutputPort("next").Connect(musicNode.GetInputPort("previous"));
            musicNode.GetOutputPort("next").Connect(frame.GetInputPort("previous"));
            frame.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            var invalid = ScriptableObject.CreateInstance<MemoryGraph>();
            float time = Time.timeScale;
            bool audio = AudioListener.pause;
            var music = new Music();
            using var presenter = new MemoryPresenter(collection, state, view, input, music, new Sounds());
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
                Assert.AreEqual(0, music.Stopped);
                Assert.IsTrue(presenter.IsPlaying && view.Visible && input.Blocked);
                Assert.AreEqual(GameplayMode.Reading, state.Mode);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsFalse(collection.Contains("first"));
                presenter.Close();
                Assert.AreEqual(1, music.Stopped);
                Assert.IsFalse(presenter.IsPlaying || view.Visible || input.Blocked);
                Assert.IsFalse(collection.Contains("first"));
                Assert.AreEqual(time, Time.timeScale);
                collection.Collect("replay", graph);
                Assert.IsTrue(presenter.Open("replay", graph, true));
                input.Escape();
                Assert.IsFalse(presenter.IsPlaying || view.Visible || input.Blocked);
                Assert.AreEqual(2, music.Stopped);
                Assert.IsTrue(presenter.Open("first", graph));
                presenter.Dispose();
                Assert.AreEqual(3, music.Stopped);
                Assert.AreEqual(time, Time.timeScale);
                Assert.AreEqual(audio, AudioListener.pause);
                Assert.IsFalse(collection.Contains("first"));
            }
            finally
            {
                presenter.Dispose();
                UnityEngine.Object.DestroyImmediate(invalid);
                DestroyGraph(graph);
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
