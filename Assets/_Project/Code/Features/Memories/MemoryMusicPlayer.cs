using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MemoryMusicPlayer : MonoBehaviour, IMemoryMusic
    {
        private AudioSource source;
        private CancellationTokenSource fade;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.ignoreListenerPause = true;
            source.Stop();
        }

        public void Execute(MemoryMusicNode node)
        {
            // Empty designer placeholders do not interrupt an already playing track.
            if (node.Action == MemoryMusicAction.Play && node.Clip == null) return;
            CancelFade();
            float target;
            if (node.Action == MemoryMusicAction.Play)
            {
                source.Stop();
                source.clip = node.Clip;
                source.loop = node.Loop;
                source.volume = node.FadeDuration > 0 ? 0 : node.Volume;
                source.Play();
                target = node.Volume;
            }
            else target = 0;

            if (node.FadeDuration <= 0)
            {
                source.volume = target;
                if (node.Action == MemoryMusicAction.Stop) StopImmediately();
                return;
            }
            fade = new CancellationTokenSource();
            FadeAsync(target, node.FadeDuration, node.Action == MemoryMusicAction.Stop, fade.Token).Forget();
        }

        private async UniTask FadeAsync(float target, float duration, bool stop, CancellationToken token)
        {
            float start = source.volume;
            float elapsed = 0;
            try
            {
                while (elapsed < duration)
                {
                    await UniTask.NextFrame(cancellationToken: token);
                    elapsed += Time.unscaledDeltaTime;
                    source.volume = Mathf.Lerp(start, target, elapsed / duration);
                }
                if (stop)
                {
                    source.Stop();
                    source.clip = null;
                }
            }
            catch (OperationCanceledException) { }
        }

        public void StopImmediately()
        {
            CancelFade();
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.volume = 0;
        }

        private void CancelFade()
        {
            fade?.Cancel();
            fade?.Dispose();
            fade = null;
        }

        private void OnDisable() => StopImmediately();
        private void OnDestroy() => CancelFade();
    }
}
