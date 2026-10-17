using System;
using Memories.Contracts;
using UnityEngine;

namespace Memories.Gameplay.UI
{
    public sealed class PausePresenter : IDisposable
    {
        private readonly IPauseInput input;
        private readonly IPauseView view;
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool disposed;
        public bool IsPaused { get; private set; }

        public PausePresenter(IPauseInput input, IPauseView view)
        {
            this.input = input;
            this.view = view;
            input.PauseRequested += Toggle;
            view.ResumeRequested += Resume;
            view.QuitRequested += Quit;
            view.Show(false);
        }

        public void Toggle()
        {
            if (disposed) return;
            if (IsPaused) { Resume(); return; }
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            IsPaused = true;
            Time.timeScale = 0;
            AudioListener.pause = true;
            input.SetPaused(true);
            view.Show(true);
        }

        public void Resume()
        {
            if (disposed || !IsPaused) return;
            RestoreTime();
            input.SetPaused(false);
            view.Show(false);
        }

        private void RestoreTime()
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            IsPaused = false;
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
            if (IsPaused) RestoreTime();
        }
    }
}
