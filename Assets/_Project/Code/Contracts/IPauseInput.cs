using System;

namespace Memories.Contracts
{
    public interface IAdvanceInput
    {
        event Action AdvanceRequested;
    }

    public interface IPauseInput
    {
        event Action PauseRequested;
        void SetPaused(bool paused);
    }
}
