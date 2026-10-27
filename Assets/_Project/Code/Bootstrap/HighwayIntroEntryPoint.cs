using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using Memories.Gameplay.UI;
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
            var fragments = new[]
            {
                new StoryTextFragment("Пахнет прелой листвой и гнилью.", 5f),
                new StoryTextFragment("Где я?", 3f),
                new StoryTextFragment("Кто… я?", 3f),
                new StoryTextFragment("Не помню, как я тут оказался.", 5f),
                new StoryTextFragment("Я что, умер?", 3f)
            };
            await texts.ShowAsync(fragments, location.Token, pauseBetweenFragments: 5f, keepVisible: true);
        }
    }
}
