using System;
using UnityEngine;

namespace Memories.Gameplay.UI
{
    public enum AudioChannel { Music, Sounds }

    public sealed class AudioVolumeSettings
    {
        private const string Prefix = "Memories.Audio.";
        public event Action Changed;
        public float Master { get; private set; }
        public float Music { get; private set; }
        public float Sounds { get; private set; }

        public AudioVolumeSettings()
        {
            Master = Read("Master");
            Music = Read("Music");
            Sounds = Read("Sounds");
            AudioListener.volume = Master;
        }

        public void SetMaster(float value) { Master = Clamp(value); AudioListener.volume = Master; Change("Master", Master); }
        public void SetMusic(float value) { Music = Clamp(value); Change("Music", Music); }
        public void SetSounds(float value) { Sounds = Clamp(value); Change("Sounds", Sounds); }
        public float Gain(AudioChannel channel) => channel == AudioChannel.Music ? Music : Sounds;
        public void Save() => PlayerPrefs.Save();
        private void Change(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            Changed?.Invoke();
        }
        private static float Read(string key) => Clamp(PlayerPrefs.GetFloat(Prefix + key, 1));
        private static float Clamp(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Mathf.Clamp01(value);
    }
}
