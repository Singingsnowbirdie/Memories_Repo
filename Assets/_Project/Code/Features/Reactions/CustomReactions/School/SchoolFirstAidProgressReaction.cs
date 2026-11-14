using System;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Задачу продвигают только действия графов начала воспоминания и подбора предметов.
    public sealed class SchoolFirstAidProgressReaction : CustomReaction, IObjectStateParticipant
    {
        private const string HintId = "school.first-aid-kit";
        [SerializeField] private ReactionGraph refreshGraph;
        private ReactionRunner runner;
        private bool started, returned;
        [Serializable] private sealed class SavedHints { public string[] active; }
        [Serializable] private sealed class State
        {
            public bool started, returned;
            public string hints;
        }
        public string StateKey => "school-first-aid";

        public void Initialize(StoryTextPresenter texts, MemoryPresenter memories, QuestInventory inventory, CustomVariableStore variables)
        {
            runner?.Dispose();
            runner = new ReactionRunner(texts, memories, this, inventory: inventory, variables: variables);
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
            // Подбор предметов явно запускает таймер, если он ещё не запущен. Его граф подсказки выбирает актуальный текст.
            if (!runner.TryRun(refreshGraph, allowDuringMemory: true))
                throw new InvalidOperationException("School first-aid reminder graph could not run.");
            GetComponent<ObjectStateTracker>()?.Capture();
        }

        public void ReturnToCoach()
        {
            returned = true;
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
            if (started && !string.IsNullOrEmpty(state.hints))
            {
                // В старых сохранениях для каждого запрошенного предмета использовался отдельный ID таймера.
                // Восстанавливаем активное напоминание без повторного запуска графа обновления.
                var hints = JsonUtility.FromJson<SavedHints>(state.hints);
                if (hints?.active != null && Array.Exists(hints.active, id =>
                    id == HintId || id == "school.ice-pack" || id == "school.ruler" ||
                    id == "school.tissues" || id == "school.return-to-coach"))
                    runner?.RestoreHints(refreshGraph, JsonUtility.ToJson(new SavedHints { active = new[] { HintId } }));
            }
        }

        private void OnDisable() => runner?.Stop();
        private void OnDestroy() => runner?.Dispose();
    }
}
