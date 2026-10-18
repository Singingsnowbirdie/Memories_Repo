using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Story;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Triggers
{
    [AddComponentMenu("Memories/Triggers/Show Story Text Event")]
    public sealed class ShowStoryTextEvent : TriggerEvent
    {
        [SerializeField] private StoryTextGraph graph;
        private StoryTextPresenter presenter;
        public override void Initialize(StoryTextPresenter texts, MemoryPresenter memories) => presenter = texts;
        public override bool TryExecute()
        {
            if (presenter == null || graph == null) return false;
            if (!graph.TryGetSequence(out var fragments, out var error))
            {
                Debug.LogWarning("Text trigger: " + error, this);
                return false;
            }
            presenter.ShowAsync(fragments, this.GetCancellationTokenOnDestroy()).Forget();
            return true;
        }
    }
}
