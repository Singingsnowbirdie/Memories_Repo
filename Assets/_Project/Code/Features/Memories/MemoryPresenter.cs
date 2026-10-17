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
        private MemoryComic comic;
        private int frameIndex;

        public MemoryPresenter(MemoryCollection collection, GameplayModeState state, IMemoryView view, IPauseInput input)
        {
            this.collection = collection;
            this.state = state;
            this.view = view;
            this.input = input;
            view.CloseRequested += Close;
            view.AdvanceRequested += Advance;
            input.PauseRequested += Close;
            view.Hide();
        }

        public bool CanOpen(string id) => !disposed && !string.IsNullOrWhiteSpace(id)
            && state.Mode == GameplayMode.Playing && !collection.Contains(id);

        public bool Open(string id, string title, string text, MemoryComic comic = null)
        {
            if (comic != null && !comic.IsValid) return false;
            if (!CanOpen(id) || !state.TryEnter(GameplayMode.Reading)) return false;
            currentId = id;
            this.comic = comic;
            frameIndex = 0;
            view.Show(comic != null ? "" : title, comic != null ? "" : text);
            if (comic != null) view.ShowFrame(comic.GetFrame(0), comic.FrameCount == 1);
            return true;
        }

        public void Advance()
        {
            if (disposed || currentId == null || comic == null || view.IsTransitioning) return;
            if (frameIndex == comic.FrameCount - 1) { Close(); return; }
            frameIndex++;
            view.ShowFrame(comic.GetFrame(frameIndex), frameIndex == comic.FrameCount - 1);
        }

        public void Close()
        {
            if (disposed || currentId == null) return;
            collection.Collect(currentId);
            currentId = null;
            comic = null;
            view.Hide();
            state.Exit(GameplayMode.Reading);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            view.CloseRequested -= Close;
            view.AdvanceRequested -= Advance;
            input.PauseRequested -= Close;
            currentId = null;
            // An interrupted viewing is not counted as collected.
            state.Exit(GameplayMode.Reading, false);
        }
    }
}
