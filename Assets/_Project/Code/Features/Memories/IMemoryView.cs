using System;

namespace Memories.Gameplay.Memories
{
    public interface IMemoryView
    {
        event Action CloseRequested;
        void Show(string title, string text);
        void Hide();
    }
}
