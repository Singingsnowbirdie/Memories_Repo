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
        public bool IsPaused => state.Mode == GameplayMode.Paused;

        public PausePresenter(IPauseInput input, IPauseView view, GameplayModeState state)
        {
            this.input = input;
            this.view = view;
            this.state = state;
            input.PauseRequested += Toggle;
            view.ResumeRequested += Resume;
            view.QuitRequested += Quit;
            view.Show(false);
        }

        public void Toggle()
        {
            if (disposed) return;
            if (IsPaused) { Resume(); return; }
            if (!state.TryEnter(GameplayMode.Paused)) return;
            view.Show(true);
        }

        public void Resume()
        {
            if (disposed || !IsPaused) return;
            state.Exit(GameplayMode.Paused);
            view.Show(false);
        }

        private void Quit()
        {
            if (disposed) return;
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
            // The player owns cursor cleanup; do not recapture it while unloading.
            state.Exit(GameplayMode.Paused, false);
        }
    }
}
