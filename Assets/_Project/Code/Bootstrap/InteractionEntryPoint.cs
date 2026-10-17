using Memories.Gameplay.Interaction;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class InteractionEntryPoint : ILateTickable
    {
        private readonly InteractionPresenter presenter;
        public InteractionEntryPoint(InteractionPresenter presenter) => this.presenter = presenter;
        public void LateTick() => presenter.Tick();
    }
}
