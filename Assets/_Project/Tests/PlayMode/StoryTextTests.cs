using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Bootstrap;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;

namespace Memories.Tests.PlayMode
{
    public sealed class StoryTextTests
    {
        private static void Timing(StoryTextView view, float slide, float fade)
        {
            typeof(StoryTextView).GetField("slideDuration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, slide);
            typeof(StoryTextView).GetField("textFadeDuration", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, fade);
        }

        [UnityTest]
        public IEnumerator PanelSlidesAndOnlyTextFadesBetweenFragments() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Story text test", typeof(RectTransform));
            var view = root.AddComponent<StoryTextView>();
            try
            {
                var panel = (RectTransform)root.transform.GetChild(0);
                var text = root.GetComponentInChildren<Text>();
                var group = text.GetComponent<CanvasGroup>();
                var reveal = view.ShowAsync("Я помню этот сад.", CancellationToken.None);
                Assert.Less(panel.anchoredPosition.y, 0);
                await UniTask.Delay(100);
                Assert.Less(panel.anchoredPosition.y, 48);
                await reveal;
                Assert.That(panel.anchoredPosition.y, Is.EqualTo(48).Within(.001f));
                Assert.AreEqual(Color.black, panel.GetComponent<Image>().color);
                Assert.AreEqual(Color.white, text.color);
                Assert.IsFalse(panel.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.IsFalse(text.raycastTarget);
                var fade = view.ChangeTextAsync("Но не помню, с кем сюда приходила.", CancellationToken.None);
                await UniTask.Delay(60);
                Assert.That(group.alpha, Is.InRange(.01f, .99f));
                Assert.AreEqual("Я помню этот сад.", text.text);
                Assert.That(panel.anchoredPosition.y, Is.EqualTo(48).Within(.001f));
                Assert.AreEqual(1, panel.GetComponent<CanvasGroup>().alpha);
                await fade;
                Assert.AreEqual("Но не помню, с кем сюда приходила.", text.text);
                Assert.AreEqual(1, group.alpha);
                await view.HideAsync(CancellationToken.None);
                Assert.IsFalse(root.GetComponent<Canvas>().enabled);
            }
            finally { UnityEngine.Object.Destroy(root); }
        });

        [UnityTest]
        public IEnumerator SequenceUsesFragmentDurationsAndStopsDuringPause() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Story timing test", typeof(RectTransform));
            var view = root.AddComponent<StoryTextView>();
            Timing(view, 0, 0);
            using var presenter = new StoryTextPresenter(view);
            float time = Time.timeScale;
            try
            {
                var text = root.GetComponentInChildren<Text>();
                var playback = presenter.ShowAsync(new[] { new StoryTextFragment("Первый", .15f), new StoryTextFragment("Второй", .2f) });
                Assert.AreEqual("Первый", text.text);
                Time.timeScale = 0;
                await UniTask.Delay(250, ignoreTimeScale: true);
                Assert.AreEqual("Первый", text.text);
                Assert.IsTrue(presenter.IsPlaying);
                Time.timeScale = 1;
                await UniTask.WaitUntil(() => text.text == "Второй");
                Assert.IsTrue(root.GetComponent<Canvas>().enabled);
                await playback;
                Assert.IsFalse(presenter.IsPlaying);
                Assert.IsFalse(root.GetComponent<Canvas>().enabled);
            }
            finally { Time.timeScale = time; presenter.Dispose(); UnityEngine.Object.Destroy(root); }
        });

        [UnityTest]
        public IEnumerator ReplacementCancellationAndDisableCannotLeaveStaleText() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Story cancellation test", typeof(RectTransform));
            var view = root.AddComponent<StoryTextView>();
            Timing(view, .03f, .03f);
            using var presenter = new StoryTextPresenter(view);
            try
            {
                var old = presenter.ShowAsync("Старый текст", 30);
                using var cancellation = new CancellationTokenSource();
                var next = presenter.ShowAsync("Новый текст", 30, cancellation.Token);
                Assert.IsTrue(await old.SuppressCancellationThrow());
                await UniTask.Delay(60);
                Assert.AreEqual("Новый текст", root.GetComponentInChildren<Text>().text);
                Assert.IsTrue(root.GetComponent<Canvas>().enabled);
                cancellation.Cancel();
                Assert.IsTrue(await next.SuppressCancellationThrow());
                Assert.IsFalse(root.GetComponent<Canvas>().enabled);
                var disabled = presenter.ShowAsync("Отключение", 30);
                await UniTask.Delay(60);
                view.enabled = false;
                Assert.IsTrue(await disabled.SuppressCancellationThrow());
                Assert.IsFalse(presenter.IsPlaying);
                view.enabled = true;
                await presenter.ShowAsync("Повторный показ", .02f);
                Assert.IsFalse(root.GetComponent<Canvas>().enabled);
                var live = presenter.ShowAsync("Продолжение", 30);
                bool invalid = false;
                try { await presenter.ShowAsync("", 1); }
                catch (ArgumentException) { invalid = true; }
                Assert.IsTrue(invalid);
                Assert.IsTrue(presenter.IsPlaying);
                presenter.Dispose();
                Assert.IsTrue(await live.SuppressCancellationThrow());
                Assert.IsFalse(root.GetComponent<Canvas>().enabled);
            }
            finally { presenter.Dispose(); UnityEngine.Object.Destroy(root); }
        });

        [UnityTest]
        public IEnumerator LocationProvidesPanelAndCancelsPlaybackWhenUnloaded() => UniTask.ToCoroutine(async () =>
        {
            const string path = "Assets/_Project/Locations/ForestIsland/ForestIslandBootstrap.unity";
            await SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(path);
            AppLifetimeScope app = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<AppLifetimeScope>(out var candidate)) app = candidate;
            Assert.IsNotNull(app);
            var session = app.Container.Resolve<GameSession>();
            try
            {
                await UniTask.WaitUntil(() => session.IsRunning);
                var presenter = session.LocationScope.Container.Resolve<StoryTextPresenter>();
                var playback = presenter.ShowAsync("Текст на острове", 30);
                await UniTask.Delay(400);
                var view = session.LocationScope.GetComponentInChildren<StoryTextView>();
                Assert.IsNotNull(view);
                Assert.IsTrue(view.GetComponent<Canvas>().enabled);
                await session.EndAsync();
                Assert.IsTrue(await playback.SuppressCancellationThrow());
                Assert.IsTrue(view == null);
            }
            finally { if (scene.isLoaded) await SceneManager.UnloadSceneAsync(scene); }
        });
    }
}
