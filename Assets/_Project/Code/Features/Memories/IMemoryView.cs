using System;
using UnityEngine;

namespace Memories.Gameplay.Memories
{
    public interface IMemoryView
    {
        event Action CloseRequested;
        event Action AdvanceRequested;
        bool IsTransitioning { get; }
        void ShowFrame(Texture2D frame, bool isLast);
        void Show(string title, string text);
        void Hide();
    }
}
