using System;
using System.Collections;
using System.Linq;
using Memories.Contracts;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Memories.Tests.PlayMode
{
    public sealed class AudioSettingsTests
    {
        private sealed class Input : IPauseInput
        {
            public event Action PauseRequested;
            public void SetPaused(bool paused) { }
            public void Escape() => PauseRequested?.Invoke();
        }

        [UnityTest]
        public IEnumerator PauseSlidersApplyIndependentGainsAndPersist()
        {
            string[] keys = { "Memories.Audio.Master", "Memories.Audio.Music", "Memories.Audio.Sounds" };
            var existed = keys.Select(PlayerPrefs.HasKey).ToArray();
            var values = keys.Select(key => PlayerPrefs.GetFloat(key)).ToArray();
            float oldVolume = AudioListener.volume;
            float oldTime = Time.timeScale;
            bool oldPause = AudioListener.pause;
            var root = new GameObject("Settings test", typeof(RectTransform));
            var musicObject = new GameObject("Music test");
            var soundObject = new GameObject("Sound test");
            PausePresenter presenter = null;
            try
            {
                var settings = new AudioVolumeSettings();
                var music = musicObject.AddComponent<AudioVolumeBinding>();
                var sounds = soundObject.AddComponent<AudioVolumeBinding>();
                music.Initialize(settings, AudioChannel.Music);
                sounds.Initialize(settings, AudioChannel.Sounds);
                music.Volume = .8f;
                sounds.Volume = .6f;
                var view = root.AddComponent<PauseMenuView>();
                var input = new Input();
                presenter = new PausePresenter(input, view, new GameplayModeState(input), settings);
                input.Escape();
                root.GetComponentsInChildren<Button>().Single(button => button.name == "Настройки").onClick.Invoke();
                var sliders = root.GetComponentsInChildren<Slider>();
                Assert.AreEqual(3, sliders.Length);
                sliders[0].value = .4f;
                sliders[1].value = .25f;
                sliders[2].value = .5f;
                Assert.That(AudioListener.volume, Is.EqualTo(.4f).Within(.001f));
                Assert.That(musicObject.GetComponent<AudioSource>().volume, Is.EqualTo(.2f).Within(.001f));
                Assert.That(soundObject.GetComponent<AudioSource>().volume, Is.EqualTo(.3f).Within(.001f));
                sliders[1].value = 0;
                music.Volume = .9f; // A fade cannot override the player's mute.
                Assert.AreEqual(0, musicObject.GetComponent<AudioSource>().volume);
                Assert.That(soundObject.GetComponent<AudioSource>().volume, Is.EqualTo(.3f).Within(.001f));
                input.Escape();
                Assert.IsTrue(presenter.IsPaused);
                Assert.AreEqual(0, root.GetComponentsInChildren<Slider>().Length);
                var restored = new AudioVolumeSettings();
                Assert.AreEqual(.4f, restored.Master);
                Assert.AreEqual(0, restored.Music);
                Assert.AreEqual(.5f, restored.Sounds);
                root.GetComponentsInChildren<Button>().Single(button => button.name == "Настройки").onClick.Invoke();
                root.GetComponentsInChildren<Button>().Single(button => button.name == "Назад").onClick.Invoke();
                Assert.IsTrue(presenter.IsPaused);
                input.Escape();
                Assert.IsFalse(presenter.IsPaused);
            }
            finally
            {
                presenter?.Dispose();
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(musicObject);
                UnityEngine.Object.Destroy(soundObject);
                for (int i = 0; i < keys.Length; i++)
                    if (existed[i]) PlayerPrefs.SetFloat(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();
                AudioListener.volume = oldVolume;
                Time.timeScale = oldTime;
                AudioListener.pause = oldPause;
            }
            yield return null;
        }
    }
}
