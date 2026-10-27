using System;

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
        event Action BackRequested;
        event Action<float> MasterChanged;
        event Action<float> MusicChanged;
        event Action<float> SoundsChanged;
        void Show(bool visible);
        void ShowRestartConfirmation(bool visible);
        void ShowCredits(bool visible);
        void ShowSettings(bool visible, float master, float music, float sounds);
    }
}
