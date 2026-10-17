using System;

namespace Memories.Gameplay.UI
{
    public interface IPauseView
    {
        event Action ResumeRequested;
        event Action QuitRequested;
        void Show(bool visible);
    }
}
