using System.Collections.Generic;
using Memories.Contracts;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
using UnityEngine;

namespace Memories.Gameplay.Triggers
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Memories/Triggers/Scene Trigger")]
    public sealed class SceneTrigger : MonoBehaviour
    {
        [SerializeField] private ReactionGraph graph;
        [SerializeField, HideInInspector] private TriggerEvent triggerEvent;
        [SerializeField, HideInInspector] private bool triggerOnce = true;
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private bool fired;
        private ReactionRunner runner;
        public bool IsRunning => runner != null && runner.IsRunning;
        public void Initialize(StoryTextPresenter texts, MemoryPresenter memories, ILocationTransition locations = null)
        {
            runner?.Dispose();
            runner = new ReactionRunner(texts, memories, this, locations);
        }
        public bool TryActivate()
        {
            if (!isActiveAndEnabled) return false;
            if (graph != null) return runner != null && runner.TryRun(graph);
            if (triggerOnce && fired) return false;
            if (triggerEvent == null || !triggerEvent.isActiveAndEnabled || !triggerEvent.TryExecute()) return false;
            fired = true;
            return true;
        }
        private void Awake()
        {
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
            runner?.Stop();
        }
    }
}
