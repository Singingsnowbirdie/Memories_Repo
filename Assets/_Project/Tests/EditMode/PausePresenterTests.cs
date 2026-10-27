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
            public void ReturnToStart() => ReturnToStartRequested?.Invoke();
            public event Action QuitRequested { add { } remove { } }
            public event Action SettingsRequested;
            public event Action BackRequested { add { } remove { } }
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

        [Test]
        public void ReturnToStartRunsOnlyFromPauseAndResumesAfterSuccess()
        {
            float oldTime = Time.timeScale;
            bool oldAudio = AudioListener.pause;
            var input = new Input();
            var view = new View();
            int calls = 0;
            bool success = false;
            var presenter = new PausePresenter(input, view, new GameplayModeState(input), new AudioVolumeSettings(),
                () => { calls++; return success; });
            try
            {
                Time.timeScale = 1;
                view.ReturnToStart();
                Assert.AreEqual(0, calls);
                input.Escape();
                view.ReturnToStart();
                Assert.AreEqual(1, calls);
                Assert.IsTrue(presenter.IsPaused);
                success = true;
                view.ReturnToStart();
                Assert.AreEqual(2, calls);
                Assert.IsFalse(presenter.IsPaused || view.Visible || input.Paused);
                Assert.AreEqual(1, Time.timeScale);
                presenter.Dispose();
                view.ReturnToStart();
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
