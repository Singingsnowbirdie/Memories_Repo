using System;
using Memories.Contracts;
using Memories.Gameplay.UI;

namespace Memories.Gameplay.Memories
{
    public sealed class MemoryPresenter : IDisposable
    {
        private readonly MemoryCollection collection;
        private readonly GameplayModeState state;
        private readonly IMemoryView view;
        private readonly IPauseInput input;
        private string currentId;
        private bool disposed;

        public MemoryPresenter(MemoryCollection collection, GameplayModeState state, IMemoryView view, IPauseInput input)
        {
            this.collection = collection;
            this.state = state;
            this.view = view;
            this.input = input;
            view.CloseRequested += Close;
            input.PauseRequested += Close;
            view.Hide();
        }

        public bool CanOpen(string id) => !disposed && !string.IsNullOrWhiteSpace(id)
            && state.Mode == GameplayMode.Playing && !collection.Contains(id);

        public bool Open(string id, string title, string text)
        {
            if (!CanOpen(id) || !state.TryEnter(GameplayMode.Reading)) return false;
            currentId = id;
            view.Show(title, text);
            return true;
        }

        public void Close()
        {
            if (disposed || currentId == null) return;
            collection.Collect(currentId);
            currentId = null;
            view.Hide();
            state.Exit(GameplayMode.Reading);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            view.CloseRequested -= Close;
            input.PauseRequested -= Close;
            currentId = null;
            // An interrupted viewing is not counted as collected.
            state.Exit(GameplayMode.Reading, false);
        }
    }
}
