using System;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Only memory-start and pickup graph actions advance this objective.
    public sealed class SchoolFirstAidProgressReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private ReactionGraph refreshGraph;
        private ReactionRunner runner;
        private bool started, returned;
        [Serializable] private sealed class State
        {
            public bool started, returned;
            public string hints;
        }
        public string StateKey => "school-first-aid";

        public void Initialize(StoryTextPresenter texts, MemoryPresenter memories, QuestInventory inventory)
        {
            runner?.Dispose();
            runner = new ReactionRunner(texts, memories, this, inventory: inventory);
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            Execute();
        }

        public override void Execute()
        {
            if (!started || returned || runner == null) return;
            // This graph contains only explicit stop/start actions and inventory branches.
            if (!runner.TryRun(refreshGraph, allowDuringMemory: true))
                throw new InvalidOperationException("School first-aid reminder graph could not run.");
            GetComponent<ObjectStateTracker>()?.Capture();
        }

        public void ReturnToCoach()
        {
            returned = true;
            runner?.StopHint("school.return-to-coach");
            GetComponent<ObjectStateTracker>()?.Capture();
        }

        public string CaptureState() => JsonUtility.ToJson(new State
        { started = started, returned = returned, hints = runner?.CaptureHints() });

        public void RestoreState(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var state = JsonUtility.FromJson<State>(value);
            started = state.started;
            returned = state.returned;
            if (started && !returned) runner?.RestoreHints(refreshGraph, state.hints);
        }

        private void OnDisable() => runner?.Stop();
        private void OnDestroy() => runner?.Dispose();
    }
}
