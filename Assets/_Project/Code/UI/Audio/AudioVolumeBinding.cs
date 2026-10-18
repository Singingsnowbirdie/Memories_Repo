using UnityEngine;

namespace Memories.Gameplay.UI
{
    // Playback and fades write Volume; the binding applies the player's category gain.
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioVolumeBinding : MonoBehaviour
    {
        private AudioSource source;
        private AudioVolumeSettings settings;
        private AudioChannel channel;
        private float volume = 1;
        public float Volume { get => volume; set { volume = Mathf.Clamp01(value); Apply(); } }
        private void Awake() { source = GetComponent<AudioSource>(); volume = source.volume; }
        public void Initialize(AudioVolumeSettings settings, AudioChannel channel)
        {
            if (this.settings != null) this.settings.Changed -= Apply;
            this.settings = settings;
            this.channel = channel;
            settings.Changed += Apply;
            Apply();
        }
        private void Apply()
        {
            if (source != null) source.volume = volume * (settings?.Gain(channel) ?? 1);
        }
        private void OnDestroy() { if (settings != null) settings.Changed -= Apply; }
    }
}
