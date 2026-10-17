using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    public sealed class MemorySoundPlayer : MonoBehaviour, IMemorySound
    {
        private sealed class Playback
        {
            public CancellationTokenSource Cancellation;
            public GameObject Root;
            public AudioSource Source;
        }
        private readonly List<Playback> active = new List<Playback>();
        private AudioVolumeSettings settings;
        public void Initialize(AudioVolumeSettings settings) => this.settings = settings;

        public async UniTask PlayAsync(MemorySoundNode node, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (node.Clip == null) return;
            var playback = new Playback { Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token) };
            active.Add(playback);
            try
            {
                var cancellation = playback.Cancellation.Token;
                if (node.Delay > 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(node.Delay), ignoreTimeScale: true, cancellationToken: cancellation);
                cancellation.ThrowIfCancellationRequested();
                playback.Root = new GameObject("Memory sound");
                playback.Root.transform.SetParent(transform, false);
                playback.Source = playback.Root.AddComponent<AudioSource>();
                playback.Source.playOnAwake = false;
                playback.Source.loop = false;
                playback.Source.spatialBlend = 0;
                playback.Source.ignoreListenerPause = true;
                playback.Source.clip = node.Clip;
                var volume = playback.Root.AddComponent<AudioVolumeBinding>();
                if (settings != null) volume.Initialize(settings, AudioChannel.Sounds);
                playback.Source.Play();
                await UniTask.Delay(TimeSpan.FromSeconds(node.Clip.length), ignoreTimeScale: true, cancellationToken: cancellation);
            }
            finally
            {
                Release(playback);
                active.Remove(playback);
                playback.Cancellation.Dispose();
            }
        }

        public void StopAll()
        {
            foreach (var playback in active.ToArray())
            {
                playback.Cancellation.Cancel();
                Release(playback);
            }
        }

        private static void Release(Playback playback)
        {
            if (playback.Source != null) playback.Source.Stop();
            if (playback.Root != null) Destroy(playback.Root);
            playback.Source = null;
            playback.Root = null;
        }
        private void OnDisable() => StopAll();
    }
}
