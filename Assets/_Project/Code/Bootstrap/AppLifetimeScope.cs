using UnityEngine;
using Memories.Gameplay.UI;
using Memories.Contracts;
using Memories.Gameplay.Player;
using Memories.Infrastructure.Input;
using VContainer;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class AppLifetimeScope : LifetimeScope
    {
        public const string DefaultLocationPath = "Assets/Scenes/FirstPersonTest/FirstPersonTest.unity";
        [SerializeField] private string initialLocationPath = DefaultLocationPath;
        [SerializeField] private bool startAutomatically = true;
        [SerializeField] private PlayerMovementSettings playerMovement = new PlayerMovementSettings();

        public bool StartAutomatically { get => startAutomatically; set => startAutomatically = value; }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ApplicationContext>(Lifetime.Singleton);
            builder.Register<AudioVolumeSettings>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<AudioVolumeSettings>());
            playerMovement.Validate();
            builder.RegisterInstance(playerMovement);
            builder.Register<PlayerInputService>(Lifetime.Singleton).As<IPlayerInput>().As<IInteractionInput>().As<IPauseInput>();
            builder.Register<GameSession>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            if (startAutomatically)
                builder.RegisterEntryPoint<BootstrapEntryPoint>().WithParameter(initialLocationPath);
        }
    }
}
