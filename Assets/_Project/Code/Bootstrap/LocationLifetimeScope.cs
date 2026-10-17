using VContainer;
using VContainer.Unity;
using Memories.Gameplay.Player;

namespace Memories.Bootstrap
{
    public sealed class LocationLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LocationContext>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<LocationContext>());
            builder.Register<PlayerMotor>(container => PlayerFactory.Create(this, container.Resolve<PlayerMovementSettings>()), Lifetime.Singleton)
                .AsSelf().As<IPlayerView>();
            builder.Register<PlayerMovementModel>(Lifetime.Singleton);
            builder.Register<PlayerPresenter>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PlayerEntryPoint>();
        }
    }
}
