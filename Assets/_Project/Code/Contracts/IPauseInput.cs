using System;

namespace Memories.Contracts
{
    public interface IPauseInput
    {
        event Action PauseRequested;
        void SetPaused(bool paused);
    }
}
