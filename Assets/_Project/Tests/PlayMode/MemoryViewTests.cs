using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Memories.Tests.PlayMode
{
    public sealed class MemoryViewTests
    {
        [UnityTest]
        public IEnumerator TransitionsShowImageAndCaptionWhilePausedAndCancelCleanly() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Memory view test", typeof(RectTransform));
            var view = root.AddComponent<MemoryView>();
            var frame = ScriptableObject.CreateInstance<MemoryFrameNode>();
            var comic = Resources.Load<MemoryComic>("Memories/FirstMemoryComic");
            float time = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                view.Show();
                foreach (MemoryFrameTransition mode in Enum.GetValues(typeof(MemoryFrameTransition)))
                {
                    Set(frame, "image", comic.GetFrame((int)mode));
                    Set(frame, "text", "Текст под кадром " + mode);
                    Set(frame, "transition", mode);
                    Set(frame, "transitionDuration", .08f);
                    float started = Time.realtimeSinceStartup;
                    await view.ShowFrameAsync(frame, CancellationToken.None);
                    if (mode != MemoryFrameTransition.Instant)
                        Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, .07f);
                    var visible = root.GetComponentsInChildren<RawImage>().Where(x => x.GetComponentInParent<CanvasGroup>().alpha > .99f).ToArray();
                    Assert.AreEqual(1, visible.Length);
                    Assert.AreSame(frame.Image, visible[0].texture);
                    Assert.AreEqual(frame.Text, visible[0].transform.parent.GetComponentInChildren<Text>().text);
                }
                Assert.AreEqual(0, root.GetComponentsInChildren<Button>(true).Length);
                using var cancellation = new CancellationTokenSource();
                var pending = view.ShowFrameAsync(frame, cancellation.Token);
                cancellation.Cancel();
                Assert.IsTrue(await pending.SuppressCancellationThrow());
                view.Hide();
                Assert.IsFalse(root.transform.GetChild(0).gameObject.activeSelf);
            }
            finally
            {
                Time.timeScale = time;
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(frame);
            }
            await UniTask.NextFrame();
        });

        private static void Set(MemoryFrameNode frame, string name, object value)
            => typeof(MemoryFrameNode).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(frame, value);
    }
}
