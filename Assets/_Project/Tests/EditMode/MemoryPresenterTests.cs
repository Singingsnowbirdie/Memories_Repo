using System;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

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
            public event Action CloseRequested;
            public event Action AdvanceRequested { add { } remove { } }
            public bool IsTransitioning => false;
            public void ShowFrame(Texture2D frame, bool isLast) { }
            public bool Visible;
            public void Show(string title, string text) => Visible = true;
            public void Hide() => Visible = false;
            public void Close() => CloseRequested?.Invoke();
        }
        private sealed class PauseView : IPauseView
        {
            public event Action ResumeRequested { add { } remove { } }
            public event Action QuitRequested { add { } remove { } }
            public void Show(bool visible) { }
        }

        [Test]
        public void CollectOnCloseRejectDuplicatesAndKeepPauseExclusive()
        {
            float time = Time.timeScale;
            bool audio = AudioListener.pause;
            var input = new Input();
            var state = new GameplayModeState(input);
            var collection = new MemoryCollection();
            var view = new View();
            using var pause = new PausePresenter(input, new PauseView(), state);
            using var memory = new MemoryPresenter(collection, state, view, input);
            try
            {
                Assert.IsFalse(memory.Open("", "", ""));
                Assert.IsTrue(memory.Open("first", "Title", "Text"));
                Assert.IsTrue(view.Visible && input.Blocked);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsFalse(collection.Contains("first"));
                Assert.IsFalse(memory.Open("second", "", ""));
                pause.Toggle();
                Assert.IsFalse(pause.IsPaused);
                input.Escape();
                Assert.IsTrue(collection.Contains("first"));
                Assert.AreEqual(GameplayMode.Playing, state.Mode);
                Assert.IsFalse(view.Visible || input.Blocked);
                Assert.AreEqual(time, Time.timeScale);
                Assert.IsFalse(memory.Open("first", "", ""));
                input.Escape();
                Assert.IsTrue(pause.IsPaused);
                Assert.IsFalse(memory.Open("second", "", ""));
                input.Escape();
                Assert.IsTrue(memory.Open("second", "", ""));
                view.Close();
                Assert.IsTrue(collection.Contains("second"));
                Assert.IsFalse(new MemoryCollection().Contains("first"));
            }
            finally
            {
                memory.Dispose();
                pause.Dispose();
                Time.timeScale = time;
                AudioListener.pause = audio;
            }
        }

        [Test]
        public void InterruptedViewingRestoresTimeWithoutCollecting()
        {
            var input = new Input();
            var state = new GameplayModeState(input);
            var collection = new MemoryCollection();
            float time = Time.timeScale;
            bool audio = AudioListener.pause;
            var memory = new MemoryPresenter(collection, state, new View(), input);
            try
            {
                memory.Open("first", "", "");
                memory.Dispose();
                Assert.AreEqual(time, Time.timeScale);
                Assert.AreEqual(audio, AudioListener.pause);
                Assert.IsFalse(collection.Contains("first"));
                input.Escape();
                Assert.IsFalse(memory.Open("first", "", ""));
            }
            finally
            {
                memory.Dispose();
                Time.timeScale = time;
                AudioListener.pause = audio;
            }
        }
    }
}
