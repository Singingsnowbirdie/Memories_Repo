using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Triggers
{
    [AddComponentMenu("Memories/Triggers/Show Memory Event")]
    public sealed class ShowMemoryEvent : TriggerEvent
    {
        [SerializeField] private MemoryGraph graph;
        [Tooltip("Unique collection ID; use the same ID as a shard to share its collected state.")]
        [SerializeField] private string memoryId;
        private MemoryPresenter presenter;
        public override void Initialize(StoryTextPresenter texts, MemoryPresenter memories) => presenter = memories;
        public override bool TryExecute() => presenter != null && presenter.Open(memoryId, graph);
    }
}
