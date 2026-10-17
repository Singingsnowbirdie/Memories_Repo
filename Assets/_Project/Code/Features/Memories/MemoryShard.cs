using Memories.Gameplay.Interaction;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryShard : Interactable
    {
        [SerializeField] private string memoryId = "test-memory-01";
        [SerializeField] private string title = "Первое воспоминание";
        [SerializeField, TextArea] private string text = "Я кого-то ждала.\nНо пока не могу вспомнить кого.";
        private MemoryPresenter presenter;
        [SerializeField] private MemoryComic comic;
        public void SetComic(MemoryComic value) => comic = value;
        private MemoryCollection collection;
        public override string Prompt => "Вспомнить";
        public override bool CanInteract => base.CanInteract && presenter != null && presenter.CanOpen(memoryId);

        public void Initialize(MemoryPresenter presenter, MemoryCollection collection)
        {
            this.presenter = presenter;
            this.collection = collection;
            RefreshVisibility();
        }

        private void Update() => RefreshVisibility();
        private void RefreshVisibility()
        {
            if (collection != null && collection.Contains(memoryId)) gameObject.SetActive(false);
        }

        public override void Interact()
        {
            if (CanInteract) presenter.Open(memoryId, title, text, comic);
        }
    }
}
