using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается явно реакцией выхода из дома после пересечения дверного проёма.
    /// После отдельной ноды поворота взгляда забирает управление,
    /// заменяет дом за спиной и зажигает фонари парами. Затем возвращает управление.
    /// При загрузке непосредственно во двор восстанавливает уже включённые фонари без вступления.
    /// </summary>
    public sealed class BackyardEntranceReaction : CustomReaction
    {
        [SerializeField] private global::Memories.Gameplay.Triggers.SceneTrigger entranceTrigger;
        [SerializeField] private Light[] lights;
        [SerializeField] private Renderer[] lanterns;
        [SerializeField] private Material litGlass;
        [SerializeField, Min(.1f)] private float pairInterval = .45f;
        private GameplayModeState state;
        private Func<UniTask> replaceHouse;
        private bool completed;
        public bool IsRunning { get; private set; }
        public global::Memories.Gameplay.Triggers.SceneTrigger EntranceTrigger => entranceTrigger;

        public void Initialize(GameplayModeState mode,
            Func<UniTask> replacement, bool alreadyEntered)
        {
            state = mode; replaceHouse = replacement;
            entranceTrigger.enabled = !alreadyEntered;
            if (alreadyEntered)
            {
                for (int i = 0; i < lights.Length; i++) LightLantern(i);
                completed = true;
            }
        }

        public override void Execute()
        {
            if (IsRunning || completed) return;
            PlayAsync().Forget();
        }

        private async UniTask PlayAsync()
        {
            IsRunning = true;
            state.BlockControls(this);
            var cancellation = this.GetCancellationTokenOnDestroy();
            try
            {
                // Игрок уже смотрит на забор: только теперь можно заменить дом.
                var replacement = replaceHouse();
                for (int i = 0; i < lights.Length; i += 2)
                {
                    LightLantern(i);
                    if (i + 1 < lights.Length) LightLantern(i + 1);
                    await UniTask.Delay(TimeSpan.FromSeconds(pairInterval), cancellationToken: cancellation);
                }
                await replacement;
                completed = true;
            }
            finally
            {
                state.ReleaseControls(this);
                IsRunning = false;
            }
        }

        private void LightLantern(int index)
        {
            lights[index].enabled = true;
            var materials = lanterns[index].sharedMaterials;
            materials[1] = litGlass;
            lanterns[index].sharedMaterials = materials;
        }
    }
}
