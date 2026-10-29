using System;
using Memories.Contracts;
using Memories.Gameplay.Memories;
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
        private bool creditsOpen;
        private bool restartConfirmationOpen;
        private bool memoriesOpen;
        private bool replaying;
        private MemoryCollection collection;
        private MemoryPresenter memories;
        private readonly AudioVolumeSettings audio;
        private readonly Func<bool> returnToStart;
        private readonly Func<bool> restart;
        public bool IsPaused => state.Mode == GameplayMode.Paused;

        public PausePresenter(IPauseInput input, IPauseView view, GameplayModeState state, AudioVolumeSettings audio,
            Func<bool> returnToStart = null, Func<bool> restart = null)
        {
            this.input = input;
            this.view = view;
            this.state = state;
            this.audio = audio;
            this.returnToStart = returnToStart;
            this.restart = restart;
            view.RestartRequested += Restart;
            view.RestartConfirmed += ConfirmRestart;
            view.RestartCancelled += CancelRestart;
            view.ReturnToStartRequested += ReturnToStart;
            view.SettingsRequested += OpenSettings;
            view.CreditsRequested += OpenCredits;
            view.MemoriesRequested += OpenMemories;
            view.MemoryRequested += ReplayMemory;
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
            if (IsPaused) { if (restartConfirmationOpen) CancelRestart(); else if (settingsOpen || creditsOpen || memoriesOpen) Back(); else Resume(); return; }
            if (!state.TryEnter(GameplayMode.Paused)) return;
            view.Show(true);
        }

        public void Resume()
        {
            if (disposed || !IsPaused) return;
            settingsOpen = false;
            creditsOpen = false;
            restartConfirmationOpen = false;
            memoriesOpen = false;
            audio.Save();
            state.Exit(GameplayMode.Paused);
            view.Show(false);
        }

        private void ReturnToStart()
        {
            if (disposed || !IsPaused || settingsOpen) return;
            if (returnToStart?.Invoke() == true) Resume();
        }

        private void Restart()
        {
            if (disposed || !IsPaused || settingsOpen) return;
            restartConfirmationOpen = true;
            view.ShowRestartConfirmation(true);
        }

        private void ConfirmRestart()
        {
            if (disposed || !IsPaused || !restartConfirmationOpen) return;
            if (restart?.Invoke() == true) Resume();
        }

        private void CancelRestart()
        {
            if (disposed || !restartConfirmationOpen) return;
            restartConfirmationOpen = false;
            view.ShowRestartConfirmation(false);
        }

        private void OpenSettings()
        {
            if (disposed || !IsPaused) return;
            settingsOpen = true;
            view.ShowSettings(true, audio.Master, audio.Music, audio.Sounds);
        }

        private void OpenCredits()
        {
            if (disposed || !IsPaused || settingsOpen || restartConfirmationOpen) return;
            creditsOpen = true;
            view.ShowCredits(true);
        }

        private void Back()
        {
            if (disposed) return;
            if (memoriesOpen)
            {
                memoriesOpen = false;
                view.ShowMemories(false, collection.Memories);
                return;
            }
            if (creditsOpen)
            {
                creditsOpen = false;
                view.ShowCredits(false);
                return;
            }
            if (disposed || !settingsOpen) return;
            settingsOpen = false;
            audio.Save();
            view.ShowSettings(false, audio.Master, audio.Music, audio.Sounds);
        }

        // Connect after both presenters have been constructed so Escape reaches pause before reading.
        public void ConnectMemories(MemoryCollection collection, MemoryPresenter memories)
        {
            if (this.memories != null) this.memories.Closed -= OnMemoryClosed;
            this.collection = collection;
            this.memories = memories;
            memories.Closed += OnMemoryClosed;
        }

        private void OpenMemories()
        {
            if (disposed || !IsPaused || settingsOpen || creditsOpen || restartConfirmationOpen || collection == null) return;
            memoriesOpen = true;
            view.ShowMemories(true, collection.Memories);
        }

        private void ReplayMemory(string id)
        {
            if (disposed || !IsPaused || !memoriesOpen || replaying || memories == null) return;
            replaying = true;
            view.Show(false);
            if (!memories.ReplayFromCollection(id)) OnMemoryClosed();
        }

        private void OnMemoryClosed()
        {
            if (disposed || !replaying) return;
            replaying = false;
            view.Show(true);
            view.ShowMemories(true, collection.Memories);
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
            view.ReturnToStartRequested -= ReturnToStart;
            view.RestartRequested -= Restart;
            view.RestartConfirmed -= ConfirmRestart;
            view.RestartCancelled -= CancelRestart;
            view.QuitRequested -= Quit;
            view.SettingsRequested -= OpenSettings;
            view.CreditsRequested -= OpenCredits;
            view.MemoriesRequested -= OpenMemories;
            view.MemoryRequested -= ReplayMemory;
            if (memories != null) memories.Closed -= OnMemoryClosed;
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
