using System;
using Memories.Contracts;
using Memories.Gameplay.UI;
using Memories.Gameplay.Player;
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
            public event Action ReturnToStartRequested;
            public event Action RestartRequested;
            public void Restart() => RestartRequested?.Invoke();
            public event Action RestartConfirmed;
            public event Action RestartCancelled;
            public void ConfirmRestart() => RestartConfirmed?.Invoke();
            public void CancelRestart() => RestartCancelled?.Invoke();
            public bool ConfirmationVisible;
            public void ShowRestartConfirmation(bool visible) => ConfirmationVisible = visible;
            public void ReturnToStart() => ReturnToStartRequested?.Invoke();
            public event Action QuitRequested { add { } remove { } }
            public event Action SettingsRequested;
            public event Action CreditsRequested;
            public void Credits() => CreditsRequested?.Invoke();
            public bool CreditsVisible;
            public void ShowCredits(bool visible) => CreditsVisible = visible;
            public event Action BackRequested;
            public void Back() => BackRequested?.Invoke();
            public event Action<float> MasterChanged { add { } remove { } }
            public event Action<float> MusicChanged { add { } remove { } }
            public event Action<float> SoundsChanged { add { } remove { } }
            public bool SettingsVisible;
            public void ShowSettings(bool visible, float master, float music, float sounds) => SettingsVisible = visible;
            public void Settings() => SettingsRequested?.Invoke();
            public bool Visible;
            public void Show(bool visible) => Visible = visible;
            public void Resume() => ResumeRequested?.Invoke();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RecoveryActionsRunOnlyFromPauseAndResumeAfterSuccess(bool restart)
        {
            float oldTime = Time.timeScale;
            bool oldAudio = AudioListener.pause;
            var input = new Input();
            var view = new View();
            int calls = 0;
            bool success = false;
            Func<bool> action = () => { calls++; return success; };
            Action request = restart ? (Action)(() => { view.Restart(); view.ConfirmRestart(); }) : view.ReturnToStart;
            var presenter = new PausePresenter(input, view, new GameplayModeState(input), new AudioVolumeSettings(),
                returnToStart: restart ? null : action, restart: restart ? action : null);
            try
            {
                Time.timeScale = 1;
                request();
                Assert.AreEqual(0, calls);
                input.Escape();
                if (restart)
                {
                    view.Restart();
                    Assert.IsTrue(view.ConfirmationVisible);
                    Assert.AreEqual(0, calls, "Opening the popup must not reset progress.");
                    input.Escape();
                    Assert.IsFalse(view.ConfirmationVisible);
                    Assert.IsTrue(presenter.IsPaused);
                    view.ConfirmRestart();
                    Assert.AreEqual(0, calls, "A dismissed popup must not allow a restart.");
                    view.Restart();
                    view.CancelRestart();
                    Assert.IsFalse(view.ConfirmationVisible);
                    Assert.IsTrue(presenter.IsPaused);
                    Assert.AreEqual(0, calls);
                }
                request();
                Assert.AreEqual(1, calls);
                Assert.IsTrue(presenter.IsPaused);
                success = true;
                request();
                Assert.AreEqual(2, calls);
                Assert.IsFalse(presenter.IsPaused || view.Visible || input.Paused);
                Assert.AreEqual(1, Time.timeScale);
                presenter.Dispose();
                request();
                Assert.AreEqual(2, calls);
            }
            finally
            {
                presenter.Dispose();
                Time.timeScale = oldTime;
                AudioListener.pause = oldAudio;
            }
        }

        [Test]
        public void RecoveryTeleportsControllerAndClearsFallingAndLook()
        {
            var root = new GameObject("Recovery test player");
            var camera = new GameObject("Recovery test camera");
            try
            {
                root.transform.position = new Vector3(0, .05f, 118);
                var settings = new PlayerMovementSettings();
                var motor = root.AddComponent<PlayerMotor>();
                motor.Initialize(settings, camera.transform);
                var model = new PlayerMovementModel(settings);
                model.ApplyGravity(false, 2);
                model.SetInitialPitch(45);
                root.transform.position = new Vector3(9, -30, 240);
                motor.Teleport(motor.InitialPosition, motor.InitialYaw);
                model.ResetMotion(motor.InitialYaw);
                Assert.AreEqual(new Vector3(0, .05f, 118), root.transform.position);
                Assert.IsTrue(root.GetComponent<CharacterController>().enabled);
                Assert.AreEqual(0, model.VerticalSpeed);
                Assert.AreEqual(0, model.Pitch);
                Assert.AreEqual(motor.InitialYaw, model.Yaw);
                Assert.Less(Quaternion.Angle(Quaternion.identity, camera.transform.localRotation), .001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camera);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EscapeAndResumeButtonRestorePreviousTimeAndAudio()
        {
            float oldTime = Time.timeScale;
            float oldVolume = AudioListener.volume;
            bool oldAudio = AudioListener.pause;
            var input = new Input();
            var view = new View();
            var presenter = new PausePresenter(input, view, new GameplayModeState(input), new AudioVolumeSettings());
            try
            {
                Time.timeScale = .5f;
                AudioListener.pause = false;
                input.Escape();
                Assert.IsTrue(presenter.IsPaused && input.Paused && view.Visible);
                Assert.AreEqual(0, Time.timeScale);
                Assert.IsTrue(AudioListener.pause);
                view.Settings();
                Assert.IsTrue(view.SettingsVisible);
                input.Escape();
                Assert.IsFalse(view.SettingsVisible);
                Assert.IsTrue(presenter.IsPaused && input.Paused);
                Assert.AreEqual(0, Time.timeScale);
                view.Credits();
                Assert.IsTrue(view.CreditsVisible);
                Assert.AreEqual(0, Time.timeScale);
                input.Escape();
                Assert.IsFalse(view.CreditsVisible);
                Assert.IsTrue(presenter.IsPaused);
                view.Credits();
                view.Back();
                Assert.IsFalse(view.CreditsVisible);
                Assert.IsTrue(presenter.IsPaused);
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
                AudioListener.volume = oldVolume;
                AudioListener.pause = oldAudio;
            }
        }
    }
}
