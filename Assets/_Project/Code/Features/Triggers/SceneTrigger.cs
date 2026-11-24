using Memories.Gameplay.Interaction;
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
    public sealed class SceneTrigger : MonoBehaviour, IConditionalObjectStateParticipant
    {
        [SerializeField] private ReactionGraph graph;
        [SerializeField, HideInInspector] private TriggerEvent triggerEvent;
        [SerializeField, HideInInspector] private bool triggerOnce = true;
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private bool fired;
        [SerializeField, InspectorName("Объекты графа")] private ReactionObjectBinding[] objectBindings = new ReactionObjectBinding[0];
        [SerializeField, InspectorName("Кастомные реакции")] private CustomReactionBinding[] customReactions = new CustomReactionBinding[0];
        private ReactionManager reactions;
        [System.Serializable] private sealed class SavedState
        {
            public bool fired;
            public string hints;
        }
        public string StateKey => "reactionHints";
        public bool RequiresPersistence => graph != null && graph.nodes.Exists(node =>
            node is ReactionNode action && action.Kind == ReactionKind.StartHints);
        public string CaptureState() => JsonUtility.ToJson(new SavedState { fired = fired, hints = reactions?.CaptureHints(this) });
        public void RestoreState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            var saved = JsonUtility.FromJson<SavedState>(state);
            // Older reminder objects stored only the active hint list after interaction.
            fired = saved.hints == null || saved.fired;
            reactions?.RestoreHints(this, saved.hints ?? state);
        }
        public bool IsRunning => reactions != null && reactions.IsRunning(this);
        public void Initialize(ReactionManager reactions)
        {
            this.reactions = reactions;
            reactions.Bind(this, graph, objectBindings, customReactions);
        }
        public bool TryActivate()
        {
            if (!isActiveAndEnabled) return false;
            if (graph != null)
            {
                if (reactions == null || (graph.RunOnce && fired)) return false;
                bool wasFired = fired;
                fired = true;
                if (!reactions.TryRun(this)) { fired = wasFired; return false; }
                GetComponent<ObjectStateTracker>()?.Capture();
                return true;
            }
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
        }
    }
}
