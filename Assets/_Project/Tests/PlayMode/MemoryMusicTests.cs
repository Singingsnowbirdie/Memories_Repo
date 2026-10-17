using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class MemoryMusicTests
    {
        [UnityTest]
        public IEnumerator MusicFadesWhilePausedAndStopsWithoutLeaking() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Memory music test");
            var player = root.AddComponent<MemoryMusicPlayer>();
            var source = root.GetComponent<AudioSource>();
            var node = ScriptableObject.CreateInstance<MemoryMusicNode>();
            // Silent in-memory test data only. No audio file is created or imported.
            var clip = AudioClip.Create("Silent test buffer", 44100, 1, 44100, false);
            float time = Time.timeScale;
            bool audio = AudioListener.pause;
            try
            {
                Time.timeScale = 0;
                AudioListener.pause = true;
                player.Execute(node);
                Assert.IsNull(source.clip);
                Assert.IsTrue(source.ignoreListenerPause);
                Assert.AreEqual(0, source.spatialBlend);
                Set(node, "clip", clip);
                Set(node, "volume", .6f);
                Set(node, "fadeDuration", .08f);
                player.Execute(node);
                Assert.AreSame(clip, source.clip);
                Assert.IsTrue(source.loop);
                Assert.AreEqual(0, source.volume);
                await UniTask.Delay(130, ignoreTimeScale: true);
                Assert.That(source.volume, Is.EqualTo(.6f).Within(.01f));
                Set(node, "clip", null);
                player.Execute(node);
                Assert.AreSame(clip, source.clip, "An empty node should not cut off existing music.");
                Set(node, "action", MemoryMusicAction.Stop);
                player.Execute(node);
                await UniTask.Delay(130, ignoreTimeScale: true);
                Assert.IsNull(source.clip);
                Assert.AreEqual(0, source.volume);
                Set(node, "action", MemoryMusicAction.Play);
                Set(node, "clip", clip);
                player.Execute(node);
                player.StopImmediately();
                await UniTask.Delay(130, ignoreTimeScale: true);
                Assert.IsNull(source.clip);
                Assert.AreEqual(0, source.volume, "Cancelled fade must not restore volume.");
                Set(node, "fadeDuration", 0f);
                player.Execute(node);
                Assert.AreEqual(.6f, source.volume);
                root.SetActive(false);
                Assert.IsNull(source.clip);
            }
            finally
            {
                Time.timeScale = time;
                AudioListener.pause = audio;
                Object.Destroy(root);
                Object.Destroy(node);
                Object.Destroy(clip);
            }
            await UniTask.NextFrame();
        });

        private static void Set(MemoryMusicNode node, string name, object value)
            => typeof(MemoryMusicNode).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, value);
    }
}
