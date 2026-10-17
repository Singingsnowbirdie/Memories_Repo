using System;
using Memories.Contracts;
using UnityEngine;

namespace Memories.Gameplay.UI
{
    public sealed class PausePresenter : IDisposable
    {
        private readonly IPauseInput input;
        private readonly IPauseView view;
        private readonly GameplayModeState state;
        private bool disposed;
        private bool settingsOpen;
        private readonly AudioVolumeSettings audio;
        public bool IsPaused => state.Mode == GameplayMode.Paused;

        public PausePresenter(IPauseInput input, IPauseView view, GameplayModeState state, AudioVolumeSettings audio)
        {
            this.input = input;
            this.view = view;
            this.state = state;
            this.audio = audio;
            view.SettingsRequested += OpenSettings;
            view.BackRequested += Back;
            view.MasterChanged += audio.SetMaster;
            view.MusicChanged += audio.SetMusic;
            view.SoundsChanged += audio.SetSounds;
            input.PauseRequested += Toggle;
            view.ResumeRequested += Resume;
            view.QuitRequested += Quit;
            view.Show(false);
        }

        public void Toggle()
        {
            if (disposed) return;
            if (IsPaused) { if (settingsOpen) Back(); else Resume(); return; }
            if (!state.TryEnter(GameplayMode.Paused)) return;
            view.Show(true);
        }

        public void Resume()
        {
            if (disposed || !IsPaused) return;
            settingsOpen = false;
            audio.Save();
            state.Exit(GameplayMode.Paused);
            view.Show(false);
        }

        private void OpenSettings()
        {
            if (disposed || !IsPaused) return;
            settingsOpen = true;
            view.ShowSettings(true, audio.Master, audio.Music, audio.Sounds);
        }

        private void Back()
        {
            if (disposed || !settingsOpen) return;
            settingsOpen = false;
            audio.Save();
            view.ShowSettings(false, audio.Master, audio.Music, audio.Sounds);
        }

        private void Quit()
        {
            if (disposed) return;
            audio.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            input.PauseRequested -= Toggle;
            view.ResumeRequested -= Resume;
            view.QuitRequested -= Quit;
            view.SettingsRequested -= OpenSettings;
            view.BackRequested -= Back;
            view.MasterChanged -= audio.SetMaster;
            view.MusicChanged -= audio.SetMusic;
            view.SoundsChanged -= audio.SetSounds;
            audio.Save();
            // The player owns cursor cleanup; do not recapture it while unloading.
            state.Exit(GameplayMode.Paused, false);
        }
    }
}
