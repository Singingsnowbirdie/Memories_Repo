using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class StoryTextInterruptionTests
    {
        private sealed class View : IStoryTextView
        {
            public readonly List<string> Shown = new List<string>();
            public string Text;
            public int Shows, Hides;
            public CancellationToken LifetimeToken => CancellationToken.None;
            public UniTask ShowAsync(string text, CancellationToken token) { Shows++; return ChangeTextAsync(text, token); }
            public UniTask ChangeTextAsync(string text, CancellationToken token)
            {
                token.ThrowIfCancellationRequested(); Text = text;
                if (!string.IsNullOrEmpty(text)) Shown.Add(text);
                return UniTask.CompletedTask;
            }
            public UniTask HideAsync(CancellationToken token) { HideImmediately(); return UniTask.CompletedTask; }
            public void HideImmediately() { Hides++; Text = ""; }
        }

        [UnityTest]
        public IEnumerator LongPhraseYieldsAfterMinimumReadTimeOnSamePanel() => UniTask.ToCoroutine(async () =>
        {
            var view = new View(); using var presenter = new StoryTextPresenter(view);
            float started = Time.time;
            var story = presenter.ShowAsync(new[] { new StoryTextFragment("long", 30), new StoryTextFragment("next", .05f) });
            var item = presenter.ShowAsync("item", .05f);
            await UniTask.Delay(1000);
            Assert.AreEqual("long", view.Text);
            Assert.IsFalse(presenter.BlocksInteractions);
            await UniTask.WaitUntil(() => view.Text == "item");
            Assert.That(Time.time - started, Is.InRange(2f, 2.6f));
            Assert.AreEqual(1, view.Shows);
            Assert.AreEqual(0, view.Hides);
            await story; await item;
            CollectionAssert.AreEqual(new[] { "long", "item", "next" }, view.Shown);
            Assert.AreEqual(1, view.Shows);
        });

        [UnityTest]
        public IEnumerator AlreadyReadPhraseYieldsImmediatelyWithoutRestartingMinimum() => UniTask.ToCoroutine(async () =>
        {
            var view = new View(); using var presenter = new StoryTextPresenter(view);
            var story = presenter.ShowAsync("long", 30);
            await UniTask.Delay(2200);
            float requested = Time.time;
            var item = presenter.ShowAsync("item", .05f);
            await UniTask.WaitUntil(() => view.Text == "item");
            Assert.Less(Time.time - requested, .5f);
            Assert.AreEqual(1, view.Shows);
            Assert.AreEqual(0, view.Hides);
            await story; await item;
        });

        [UnityTest]
        public IEnumerator ItemMessageInterruptsPauseAndOriginalSequenceResumes() => UniTask.ToCoroutine(async () =>
        {
            var view = new View(); using var presenter = new StoryTextPresenter(view);
            var original = presenter.ShowAsync(new[] { new StoryTextFragment("A", .05f), new StoryTextFragment("B", .05f) }, pauseBetweenFragments: .35f);
            Assert.IsFalse(presenter.BlocksInteractions);
            await UniTask.WaitUntil(() => view.Text == "");
            await UniTask.Delay(50);
            var item = presenter.ShowAsync("item", .15f);
            await item;
            float resumed = Time.time;
            Assert.IsTrue(presenter.IsPlaying);
            await original;
            Assert.Greater(Time.time - resumed, .2f, "The remaining pause must not elapse during the item message.");
            CollectionAssert.AreEqual(new[] { "A", "item", "B" }, view.Shown);
            Assert.IsFalse(presenter.BlocksInteractions);
        });

        [UnityTest]
        public IEnumerator InteractionDuringVisibleTextRunsBeforeNextFragmentWithoutCancellingStory() => UniTask.ToCoroutine(async () =>
        {
            var view = new View(); using var presenter = new StoryTextPresenter(view);
            var story = presenter.ShowAsync(new[] { new StoryTextFragment("A", .15f), new StoryTextFragment("B", .05f) });
            var item = presenter.ShowAsync("item", .05f);
            Assert.AreEqual("A", view.Text);
            Assert.IsFalse(presenter.BlocksInteractions);
            await story; await item;
            CollectionAssert.AreEqual(new[] { "A", "item", "B" }, view.Shown);
        });

        [UnityTest]
        public IEnumerator UninterruptibleStoryKeepsLockDuringPauseAndQueuesOtherText() => UniTask.ToCoroutine(async () =>
        {
            var view = new View(); using var presenter = new StoryTextPresenter(view);
            var story = presenter.ShowAsync(new[] { new StoryTextFragment("A", .05f), new StoryTextFragment("B", .05f) }, pauseBetweenFragments: .15f, blockInteractionsForWholeSequence: true);
            await UniTask.WaitUntil(() => view.Text == "");
            Assert.IsTrue(presenter.BlocksInteractions);
            var item = presenter.ShowAsync("item", .05f);
            await story; await item;
            CollectionAssert.AreEqual(new[] { "A", "B", "item" }, view.Shown);
        });

        [UnityTest]
        public IEnumerator ConcurrentRequestsNeverReplaceVisibleTextAndStopCancelsAll() => UniTask.ToCoroutine(async () =>
        {
            var view = new View(); using var presenter = new StoryTextPresenter(view);
            var first = presenter.ShowAsync("first", .05f);
            var second = presenter.ShowAsync("second", .05f);
            var third = presenter.ShowAsync("third", .05f);
            Assert.AreEqual("first", view.Text);
            await first; await second; await third;
            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, view.Shown);
            var parent = presenter.ShowAsync(new[] { new StoryTextFragment("parent", .02f), new StoryTextFragment("never", 1) }, pauseBetweenFragments: 2);
            await UniTask.WaitUntil(() => view.Text == "");
            var child = presenter.ShowAsync("child", 10);
            await UniTask.WaitUntil(() => view.Text == "child");
            var pending = presenter.ShowAsync("pending", 10);
            presenter.Stop();
            Assert.IsTrue(await parent.SuppressCancellationThrow());
            Assert.IsTrue(await child.SuppressCancellationThrow());
            Assert.IsTrue(await pending.SuppressCancellationThrow());
            Assert.IsFalse(presenter.IsPlaying);
            await presenter.ShowAsync("fresh", .02f);
            Assert.AreEqual("fresh", view.Shown[view.Shown.Count - 1]);
        });
    }
}
