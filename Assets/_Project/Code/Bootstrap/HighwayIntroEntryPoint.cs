using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
using Memories.Gameplay.Story;
using UnityEngine;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class HighwayIntroEntryPoint : ITickable
    {
        private readonly PlayerMotor player;
        private readonly StoryTextPresenter texts;
        private readonly LocationContext location;
        private const float HighwayEndZ = 300f;
        private readonly float firstTextZ;
        private bool started;

        public HighwayIntroEntryPoint(PlayerMotor player, StoryTextPresenter texts, LocationContext location)
        {
            this.player = player;
            this.texts = texts;
            this.location = location;
            firstTextZ = Mathf.Lerp(player.transform.position.z, HighwayEndZ, 1f / 3f);
        }

        public void Tick()
        {
            if (started || location.IsDisposed || Time.timeScale == 0) return;
            if (player.transform.position.z < firstTextZ) return;
            started = true;
            PlayAsync().Forget(Debug.LogException);
        }

        private async UniTask PlayAsync()
        {
            var graph = Resources.Load<StoryTextGraph>("HighwayIntroText");
            if (graph == null)
                throw new System.InvalidOperationException("Missing HighwayIntroText story graph.");
            if (!graph.TryGetSequence(out var fragments, out var error))
                throw new System.InvalidOperationException(error);
            await texts.ShowAsync(fragments, location.Token, pauseBetweenFragments: 5f, keepVisible: true);
        }
    }
}
