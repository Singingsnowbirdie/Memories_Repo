using Memories.Contracts;
using UnityEngine;

namespace Memories.Gameplay.UI
{
    public enum GameplayMode { Playing, Paused, Reading }

    public sealed class GameplayModeState
    {
        private readonly IPauseInput input;
        private float previousTimeScale;
        private bool previousAudioPause;
        public GameplayMode Mode { get; private set; }
        public GameplayModeState(IPauseInput input) => this.input = input;

        public bool TryEnter(GameplayMode mode)
        {
            if (mode == GameplayMode.Playing || Mode != GameplayMode.Playing) return false;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            Mode = mode;
            Time.timeScale = 0;
            AudioListener.pause = true;
            input.SetPaused(true);
            return true;
        }

        public void Exit(GameplayMode mode, bool restoreControl = true)
        {
            if (Mode != mode || mode == GameplayMode.Playing) return;
            Mode = GameplayMode.Playing;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            if (restoreControl) input.SetPaused(false);
        }
    }
}
