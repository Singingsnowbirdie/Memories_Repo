using Memories.Gameplay.Player;
using UnityEngine;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class PlayerEntryPoint : IStartable, ITickable
    {
        private readonly PlayerPresenter presenter;
        public PlayerEntryPoint(PlayerPresenter presenter) => this.presenter = presenter;
        public void Start() => presenter.Start();
        public void Tick() => presenter.Tick(Time.deltaTime);
    }
}
