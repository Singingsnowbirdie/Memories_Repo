using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using Memories.Gameplay.Player;
using UnityEngine;

namespace Memories.Gameplay.Triggers
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Memories/Triggers/Scene Trigger")]
    public sealed class SceneTrigger : MonoBehaviour
    {
        [SerializeField] private TriggerGraph graph;
        // Retain older serialized scenes until they are migrated to a graph.
        [SerializeField, HideInInspector] private TriggerEvent triggerEvent;
        [SerializeField, HideInInspector] private bool triggerOnce = true;
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private bool fired;
        private StoryTextPresenter texts;
        private MemoryPresenter memories;
        private CancellationTokenSource playback;
        public bool IsRunning => playback != null;

        public void Initialize(StoryTextPresenter textPresenter, MemoryPresenter memoryPresenter)
        {
            texts = textPresenter;
            memories = memoryPresenter;
        }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled || IsRunning) return false;
            if (graph == null)
            {
                if (triggerOnce && fired) return false;
                if (triggerEvent == null || !triggerEvent.isActiveAndEnabled || !triggerEvent.TryExecute()) return false;
                fired = true;
                return true;
            }
            if ((graph.TriggerOnce && fired) || texts == null || memories == null) return false;
            if (!graph.TryGetSequence(out var actions, out var error))
            { Debug.LogWarning("Триггер не запущен: " + error, this); return false; }
            // Do not consume a one-shot while another memory or pause owns the player.
            if (!memories.CanStartPlayback) return false;
            fired = true;
            playback = new CancellationTokenSource();
            RunAsync(actions, playback).Forget();
            return true;
        }

        private async UniTask RunAsync(List<TriggerActionNode> actions, CancellationTokenSource source)
        {
            bool ownsMemory = false;
            try
            {
                foreach (var action in actions)
                {
                    source.Token.ThrowIfCancellationRequested();
                    if (action.Kind == TriggerActionKind.StoryText)
                        await texts.ShowAsync(action.TextGraph, source.Token);
                    else
                    {
                        // Other triggers may already be showing a memory; keep sequence order.
                        await UniTask.WaitUntil(() => memories.CanOpen(action.MemoryId, true), cancellationToken: source.Token);
                        if (!memories.Open(action.MemoryId, action.MemoryGraph, true)) return;
                        ownsMemory = true;
                        await UniTask.WaitUntil(() => !memories.IsPlaying, cancellationToken: source.Token);
                        ownsMemory = false;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { Debug.LogException(exception, this); }
            finally
            {
                if (ownsMemory) memories.Close();
                if (playback == source) playback = null;
                source.Dispose();
            }
        }

        private void Awake()
        {
            // Keep the designer's collider shape; a kinematic body guarantees trigger callbacks.
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            if (triggerEvent == null) triggerEvent = GetComponent<TriggerEvent>();
            var volume = GetComponent<Collider>();
            if (volume == null || !volume.isTrigger)
                Debug.LogWarning("Scene Trigger requires a Collider with Is Trigger enabled.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled || other.GetComponentInParent<PlayerMotor>() == null) return;
            occupants.RemoveWhere(item => item == null || !item.enabled || !item.gameObject.activeInHierarchy);
            if (!occupants.Add(other) || occupants.Count != 1) return;
            TryActivate();
        }

        private void OnTriggerExit(Collider other) => occupants.Remove(other);
        private void OnDisable()
        {
            occupants.Clear();
            playback?.Cancel();
        }
    }
}
