using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class MemoryLibraryTests
    {
        private sealed class Input : IPauseInput
        {
            public event Action PauseRequested;
            public bool Blocked;
            public int Unblocks;
            public void SetPaused(bool paused) { Blocked = paused; if (!paused) Unblocks++; }
            public void Escape() => PauseRequested?.Invoke();
        }
        private sealed class MemoryView : IMemoryView
        {
            public bool Visible;
            public void Show() => Visible = true;
            public void Hide() => Visible = false;
            public UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token) => UniTask.WaitUntilCanceled(token);
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
        private sealed class PauseView : IPauseView
        {
            public event Action ResumeRequested { add { } remove { } }
            public event Action ReturnToStartRequested { add { } remove { } }
            public event Action RestartRequested { add { } remove { } }
            public event Action RestartConfirmed { add { } remove { } }
            public event Action RestartCancelled { add { } remove { } }
            public event Action QuitRequested { add { } remove { } }
            public event Action SettingsRequested { add { } remove { } }
            public event Action CreditsRequested { add { } remove { } }
            public event Action MemoriesRequested;
            public event Action<string> MemoryRequested;
            public event Action BackRequested { add { } remove { } }
            public event Action<float> MasterChanged { add { } remove { } }
            public event Action<float> MusicChanged { add { } remove { } }
            public event Action<float> SoundsChanged { add { } remove { } }
            public bool Visible, LibraryVisible;
            public void Show(bool visible) { Visible = visible; LibraryVisible = false; }
            public void ShowRestartConfirmation(bool visible) { }
            public void ShowCredits(bool visible) { }
            public void ShowSettings(bool visible, float master, float music, float sounds) { }
            public void ShowMemories(bool visible, IReadOnlyList<MemoryCollection.FoundMemory> memories) => LibraryVisible = visible;
            public void OpenLibrary() => MemoriesRequested?.Invoke();
            public void Replay(string id) => MemoryRequested?.Invoke(id);
        }

        private static MemoryGraph Graph => AssetDatabase.LoadAssetAtPath<MemoryGraph>(
            "Assets/_Project/Configs/Memories/TVBaseballMemoryGraph.asset");

        [Test]
        public void CollectionPersistsGraphReferencesAndDeduplicatesWithoutUnlockingOtherMemories()
        {
            string key = "Tests.MemoryLibrary." + Guid.NewGuid().ToString("N");
            var graph = Graph;
            var catalog = new[] { new MemoryCatalog.Entry { key = "graph-key", graph = graph } };
            try
            {
                var collection = new MemoryCollection(catalog, true, key);
                Assert.IsEmpty(collection.Memories);
                Assert.IsTrue(collection.Collect("found", graph));
                Assert.IsFalse(collection.Collect("found", graph));
                var restored = new MemoryCollection(catalog, true, key);
                Assert.IsTrue(restored.Contains("found"));
                Assert.IsFalse(restored.Contains("unseen"));
                Assert.AreEqual(1, restored.Memories.Count);
                Assert.AreSame(graph, restored.Memories[0].Graph);
                Assert.AreEqual("Бейсбол в гостиной", restored.Memories[0].Title);
                Assert.IsNotNull(restored.Memories[0].Cover);
                var isolated = new MemoryCollection(catalog, false, key);
                Assert.IsEmpty(isolated.Memories, "Editor test sessions must not load real progress.");
                isolated.Collect("test-only", graph);
                Assert.IsFalse(new MemoryCollection(catalog, true, key).Contains("test-only"));
                MemoryCollection.Clear(key);
                Assert.IsEmpty(new MemoryCollection(catalog, true, key).Memories);
            }
            finally { PlayerPrefs.DeleteKey(key); }
        }

        [Test]
        public void ReplayEscapeReturnsToLibraryWithoutResumingGameplayAndRejectsUnfoundMemory()
        {
            float time = Time.timeScale;
            bool audio = AudioListener.pause;
            var input = new Input();
            var state = new GameplayModeState(input);
            var view = new PauseView();
            var memoryView = new MemoryView();
            var collection = new MemoryCollection();
            collection.Collect("found", Graph);
            using var pause = new PausePresenter(input, view, state, new AudioVolumeSettings());
            using var memories = new MemoryPresenter(collection, state, memoryView, input, new Music(), new Sounds());
            pause.ConnectMemories(collection, memories);
            try
            {
                Time.timeScale = .5f;
                input.Escape();
                view.OpenLibrary();
                Assert.IsTrue(view.LibraryVisible);
                view.Replay("unseen");
                Assert.IsTrue(view.Visible && view.LibraryVisible);
                Assert.IsFalse(memories.IsPlaying);
                view.Replay("found");
                Assert.IsTrue(memories.IsPlaying && memoryView.Visible);
                Assert.IsFalse(view.Visible);
                Assert.AreEqual(GameplayMode.Reading, state.Mode);
                input.Escape();
                Assert.IsFalse(memories.IsPlaying || memoryView.Visible);
                Assert.IsTrue(view.Visible && view.LibraryVisible && input.Blocked);
                Assert.AreEqual(GameplayMode.Paused, state.Mode);
                Assert.AreEqual(0, Time.timeScale);
                Assert.AreEqual(0, input.Unblocks);
                Assert.AreEqual(1, collection.Memories.Count);
                input.Escape();
                Assert.IsFalse(view.LibraryVisible);
                Assert.AreEqual(GameplayMode.Paused, state.Mode);
                input.Escape();
                Assert.AreEqual(GameplayMode.Playing, state.Mode);
                Assert.AreEqual(.5f, Time.timeScale);
                Assert.AreEqual(1, input.Unblocks);
            }
            finally
            {
                memories.Dispose(); pause.Dispose();
                Time.timeScale = time; AudioListener.pause = audio;
            }
        }

        [Test]
        public void InterruptedDiscoveryIsNotAddedToLibrary()
        {
            var input = new Input();
            var collection = new MemoryCollection();
            using var memories = new MemoryPresenter(collection, new GameplayModeState(input), new MemoryView(), input, new Music(), new Sounds());
            Assert.IsTrue(memories.Open("interrupted", Graph));
            memories.Close();
            Assert.IsFalse(collection.Contains("interrupted"));
            Assert.IsEmpty(collection.Memories);
        }
    }
}
