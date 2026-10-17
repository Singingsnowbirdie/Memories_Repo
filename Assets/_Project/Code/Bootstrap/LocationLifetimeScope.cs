using VContainer;
using VContainer.Unity;
using Memories.Gameplay.Player;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.UI;
using UnityEngine;

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
            builder.Register<InteractionProbe>(container => new InteractionProbe(
                container.Resolve<PlayerMotor>().GetComponentInChildren<Camera>()), Lifetime.Singleton);
            builder.Register<IInteractionView>(container =>
            {
                var hud = new GameObject("Interaction HUD", typeof(RectTransform));
                hud.transform.SetParent(transform, false);
                return hud.AddComponent<InteractionHud>();
            }, Lifetime.Singleton);
            builder.Register<InteractionPresenter>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InteractionEntryPoint>();
        }
    }
}
