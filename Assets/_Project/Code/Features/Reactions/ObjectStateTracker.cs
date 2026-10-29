using System;
using System.Collections.Generic;
using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    [AddComponentMenu("")]
    public sealed class ObjectStateTracker : MonoBehaviour
    {
        [Serializable] private sealed class Part { public string key; public string value; }
        [Serializable] private sealed class State
        {
            public bool active;
            public bool interactionEnabled;
            public List<Part> parts = new List<Part>();
        }
        private ObjectStateStore store;
        private string id, previous;
        private Interactable interactable;
        private readonly Dictionary<string, IObjectStateParticipant> participants = new Dictionary<string, IObjectStateParticipant>();
        private bool restoring;
        private bool previousActive, previousInteraction;

        public void Initialize(ObjectStateStore store, string id, string saved)
        {
            this.store = store;
            this.id = id;
            hideFlags = HideFlags.HideInInspector;
            interactable = GetComponent<Interactable>();
            participants.Clear();
            foreach (var component in GetComponents<MonoBehaviour>())
                if (component is IObjectStateParticipant participant) participants.Add(participant.StateKey, participant);
            restoring = true;
            try
            {
                if (!string.IsNullOrEmpty(saved))
                {
                    var state = JsonUtility.FromJson<State>(saved);
                    if (interactable != null) interactable.SetInteractionEnabled(state.interactionEnabled);
                    foreach (var part in state.parts)
                        if (participants.TryGetValue(part.key, out var participant)) participant.RestoreState(part.value);
                    if (GetComponent<SavedObjectVisibility>() == null)
                        gameObject.SetActive(state.active);
                }
                previous = Snapshot();
                previousActive = gameObject.activeSelf;
                previousInteraction = interactable == null || interactable.InteractionEnabled;
            }
            finally { restoring = false; }
        }

        private string Snapshot()
        {
            var state = new State { active = gameObject.activeSelf, interactionEnabled = interactable == null || interactable.InteractionEnabled };
            foreach (var pair in participants) state.parts.Add(new Part { key = pair.Key, value = pair.Value.CaptureState() });
            return JsonUtility.ToJson(state);
        }

        public void Capture()
        {
            if (store == null || restoring) return;
            bool active = gameObject.activeSelf;
            bool interaction = interactable == null || interactable.InteractionEnabled;
            if (participants.Count == 0 && active == previousActive && interaction == previousInteraction) return;
            string current = Snapshot();
            if (current == previous) return;
            previous = current;
            previousActive = active;
            previousInteraction = interaction;
            store.Write(id, current);
        }
        private void OnEnable() => Capture();
        private void OnDisable() => Capture();
        internal void Detach() => store = null;
    }
}
