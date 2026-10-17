using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class MemorySoundTests
    {
        private sealed class Input : IPauseInput
        {
            public event Action PauseRequested { add { } remove { } }
            public void SetPaused(bool paused) { }
        }
        private sealed class View : IMemoryView
        {
            public bool FrameShown;
            public void Show() { }
            public void Hide() { }
            public UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token)
            { FrameShown = true; return UniTask.WaitUntilCanceled(token); }
        }
        private sealed class Music : IMemoryMusic
        {
            public void Execute(MemoryMusicNode node) { }
            public void StopImmediately() { }
        }
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [UnityTest]
        public IEnumerator GraphWaitsOnlyWhenRequested() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Sound test");
            var player = root.AddComponent<MemorySoundPlayer>();
            var clip = AudioClip.Create("Silent test buffer", 8820, 1, 44100, false);
            var graph = ScriptableObject.CreateInstance<MemoryGraph>();
            var start = graph.AddNode<MemoryStartNode>();
            var sound = graph.AddNode<MemorySoundNode>();
            var frame = graph.AddNode<MemoryFrameNode>();
            var end = graph.AddNode<MemoryEndNode>();
            start.GetOutputPort("next").Connect(sound.GetInputPort("previous"));
            sound.GetOutputPort("next").Connect(frame.GetInputPort("previous"));
            frame.GetOutputPort("next").Connect(end.GetInputPort("previous"));
            Set(frame, "image", Texture2D.whiteTexture);
            Set(sound, "clip", clip);
            Set(sound, "delay", .1f);
            float time = Time.timeScale;
            bool paused = AudioListener.pause;
            try
            {
                Assert.IsTrue(graph.TryGetSequence(out _, out var error), error);
                Set(sound, "delay", float.NaN);
                Assert.IsFalse(graph.TryGetSequence(out _, out _));
                Set(sound, "delay", .1f);
                foreach (bool wait in new[] { true, false })
                {
                    Set(sound, "waitForCompletion", wait);
                    var input = new Input();
                    var view = new View();
                    using var presenter = new MemoryPresenter(new MemoryCollection(), new GameplayModeState(input), view, input, new Music(), player);
                    Assert.IsTrue(presenter.Open("test", graph));
                    Assert.AreEqual(!wait, view.FrameShown);
                    await UniTask.Delay(150, ignoreTimeScale: true);
                    var source = root.GetComponentInChildren<AudioSource>();
                    Assert.IsNotNull(source);
                    Assert.IsTrue(source.ignoreListenerPause);
                    Assert.IsFalse(source.loop);
                    Assert.AreEqual(0, Time.timeScale);
                    if (wait)
                    {
                        Assert.IsFalse(view.FrameShown);
                        await UniTask.Delay(220, ignoreTimeScale: true);
                        Assert.IsTrue(view.FrameShown);
                    }
                    presenter.Close();
                    await UniTask.NextFrame();
                    Assert.AreEqual(0, root.GetComponentsInChildren<AudioSource>().Length);
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(clip);
                foreach (var node in graph.nodes) UnityEngine.Object.Destroy(node);
                UnityEngine.Object.Destroy(graph);
                Time.timeScale = time;
                AudioListener.pause = paused;
            }
        });

        [UnityTest]
        public IEnumerator EmptyClipAndCancelledDelaysDoNotStartSounds() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Delayed sound test");
            var player = root.AddComponent<MemorySoundPlayer>();
            var node = ScriptableObject.CreateInstance<MemorySoundNode>();
            var clip = AudioClip.Create("Silent test buffer", 44100, 1, 44100, false);
            try
            {
                Set(node, "delay", .1f);
                var empty = player.PlayAsync(node, CancellationToken.None);
                Assert.AreEqual(UniTaskStatus.Succeeded, empty.Status);
                await empty;
                Set(node, "clip", clip);
                var first = player.PlayAsync(node, CancellationToken.None).SuppressCancellationThrow();
                var second = player.PlayAsync(node, CancellationToken.None).SuppressCancellationThrow();
                player.StopAll();
                Assert.IsTrue(await first);
                Assert.IsTrue(await second);
                await UniTask.Delay(150, ignoreTimeScale: true);
                Assert.AreEqual(0, root.GetComponentsInChildren<AudioSource>().Length);
                Set(node, "delay", 0f);
                var active = player.PlayAsync(node, CancellationToken.None).SuppressCancellationThrow();
                Assert.AreEqual(1, root.GetComponentsInChildren<AudioSource>().Length);
                root.SetActive(false);
                Assert.IsTrue(await active);
                await UniTask.NextFrame();
                Assert.AreEqual(0, root.GetComponentsInChildren<AudioSource>(true).Length);
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(node);
                UnityEngine.Object.Destroy(clip);
            }
        });
    }
}
