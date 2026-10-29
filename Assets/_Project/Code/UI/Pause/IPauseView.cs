using System;
using System.Collections.Generic;
using Memories.Gameplay.Memories;

namespace Memories.Gameplay.UI
{
    public interface IPauseView
    {
        event Action ResumeRequested;
        event Action ReturnToStartRequested;
        event Action RestartRequested;
        event Action RestartConfirmed;
        event Action RestartCancelled;
        event Action QuitRequested;
        event Action SettingsRequested;
        event Action CreditsRequested;
        event Action MemoriesRequested;
        event Action<string> MemoryRequested;
        event Action BackRequested;
        event Action<float> MasterChanged;
        event Action<float> MusicChanged;
        event Action<float> SoundsChanged;
        void Show(bool visible);
        void ShowRestartConfirmation(bool visible);
        void ShowCredits(bool visible);
        void ShowMemories(bool visible, IReadOnlyList<MemoryCollection.FoundMemory> memories);
        void ShowSettings(bool visible, float master, float music, float sounds);
    }
}
