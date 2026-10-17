using System;
using Memories.Contracts;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class PausePresenterTests
    {
        private sealed class Input : IPauseInput
        {
            public event Action PauseRequested;
            public bool Paused;
            public void SetPaused(bool paused) => Paused = paused;
            public void Escape() => PauseRequested?.Invoke();
        }
        private sealed class View : IPauseView
        {
            public event Action ResumeRequested;
            public event Action QuitRequested { add { } remove { } }
            public bool Visible;
            public void Show(bool visible) => Visible = visible;
            public void Resume() => ResumeRequested?.Invoke();
        }

        [Test]
        public void EscapeAndResumeButtonRestorePreviousTimeAndAudio()
        {
            float oldTime = Time.timeScale;
            bool oldAudio = AudioListener.pause;
            var input = new Input();
            var view = new View();
            var presenter = new PausePresenter(input, view);
            try
            {
                Time.timeScale = .5f;
                AudioListener.pause = false;
                input.Escape();
                Assert.IsTrue(presenter.IsPaused && input.Paused && view.Visible);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsTrue(AudioListener.pause);
                view.Resume();
                Assert.IsFalse(presenter.IsPaused || input.Paused || view.Visible);
                Assert.AreEqual(.5f, Time.timeScale);
                Assert.IsFalse(AudioListener.pause);
                input.Escape();
                input.Escape();
                Assert.IsFalse(presenter.IsPaused);
                Assert.AreEqual(.5f, Time.timeScale);
                AudioListener.pause = true;
                input.Escape();
                presenter.Dispose();
                Assert.AreEqual(.5f, Time.timeScale);
                Assert.IsTrue(AudioListener.pause);
                input.Escape();
                Assert.AreEqual(.5f, Time.timeScale);
            }
            finally
            {
                presenter.Dispose();
                Time.timeScale = oldTime;
                AudioListener.pause = oldAudio;
            }
        }
    }
}
