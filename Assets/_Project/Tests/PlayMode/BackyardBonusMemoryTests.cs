#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Memories.Tests.PlayMode
{
    public sealed class BackyardBonusMemoryTests
    {
        private sealed class Input : IPauseInput, IAdvanceInput
        {
            public event Action PauseRequested;
            public event Action AdvanceRequested;
            public void SetPaused(bool paused) { }
            public void Advance() => AdvanceRequested?.Invoke();
        }
        private sealed class View : IMemoryView
        {
            public int Frames;
            public void Show() { }
            public void Hide() { }
            public UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token)
            { Frames++; return UniTask.CompletedTask; }
        }
        private sealed class Audio : IMemoryMusic, IMemorySound
        {
            public void Execute(MemoryMusicNode node) { }
            public void StopImmediately() { }
            public void StopAll() { }
            public UniTask PlayAsync(MemorySoundNode node, CancellationToken token) => UniTask.CompletedTask;
        }

        [UnityTest]
        public IEnumerator BonusCollectsOnceAndRestoresDisabledInteraction()
        {
            var graph = AssetDatabase.LoadAssetAtPath<ReactionGraph>("Assets/_Project/Configs/Reactions/Backyard/Ben_SeatedBonusMemory.asset");
            Assert.IsTrue(graph.TryGetSequence(out var actions, out var error), error);
            CollectionAssert.AreEqual(new[] { ReactionKind.Memory, ReactionKind.ObjectInteractivity }, actions.Select(a => a.Kind));
            Assert.IsFalse(actions[1].InteractionEnabled);
            Assert.IsTrue(string.IsNullOrEmpty(actions[1].ObjectKey));
            var memory = actions[0].MemoryGraph;
            Assert.IsTrue(memory.TryGetSequence(out var sequence, out error), error);
            Assert.AreEqual(5, sequence.Count);
            Assert.AreEqual(5, sequence.OfType<MemoryFrameNode>().Select(f => f.Image).Distinct().Count());
            var key = "Tests.Bonus." + Guid.NewGuid();
            var entries = new[] { new MemoryCatalog.Entry { key = "bonus", graph = memory } };
            var collection = new MemoryCollection(entries, true, key + ".memories");
            var input = new Input(); var view = new View(); var audio = new Audio();
            var modes = new GameplayModeState(input);
            var presenter = new MemoryPresenter(collection, modes, view, input, audio, audio);
            var store = new ObjectStateStore(true, key);
            var owner = new GameObject("Bonus test owner");
            var interactive = owner.AddComponent<ReactionInteractable>();
            interactive.Configure(graph, "Bonus"); interactive.Initialize(null, presenter);
            store.Register(owner, "ben");
            try
            {
                Assert.IsTrue(interactive.CanInteract);
                interactive.Interact();
                for (int frame = 1; frame <= 5; frame++)
                {
                    for (int wait = 0; wait < 30 && view.Frames < frame; wait++) yield return null;
                    Assert.AreEqual(frame, view.Frames);
                    input.Advance();
                    yield return null; yield return null;
                }
                for (int wait = 0; wait < 30 && interactive.IsRunning; wait++) yield return null;
                Assert.IsTrue(collection.Contains(actions[0].MemoryId));
                Assert.AreEqual(1, collection.Memories.Count);
                Assert.IsFalse(interactive.CanInteract);
                Assert.IsTrue(owner.activeSelf);
                interactive.Interact();
                Assert.IsFalse(presenter.IsPlaying);
                store.Dispose();
                UnityEngine.Object.DestroyImmediate(owner);
                owner = new GameObject("Restored bonus owner");
                interactive = owner.AddComponent<ReactionInteractable>();
                interactive.Configure(graph, "Bonus"); interactive.Initialize(null, presenter);
                store = new ObjectStateStore(true, key); store.Register(owner, "ben");
                Assert.IsFalse(interactive.CanInteract);
                var restored = new MemoryCollection(entries, true, key + ".memories");
                Assert.AreEqual(memory, restored.Memories.Single().Graph);
                Assert.IsTrue(modes.TryEnter(GameplayMode.Paused));
                Assert.IsTrue(presenter.ReplayFromCollection(actions[0].MemoryId));
                presenter.Close();
                Assert.IsFalse(interactive.CanInteract);
            }
            finally
            {
                presenter.Dispose(); modes.Exit(GameplayMode.Paused); store.Dispose(); UnityEngine.Object.DestroyImmediate(owner);
                PlayerPrefs.DeleteKey(key); PlayerPrefs.DeleteKey(key + ".memories"); PlayerPrefs.Save();
            }
        }
    }
}
#endif
