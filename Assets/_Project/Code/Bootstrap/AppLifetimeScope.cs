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
        [SerializeField] private string[] additionalLocationScenes = System.Array.Empty<string>();
        [SerializeField] private bool startAutomatically = true;
        [SerializeField] private PlayerMovementSettings playerMovement = new PlayerMovementSettings();

#if UNITY_EDITOR
        public bool UseOpenLocationForEditorPlay { get; set; }
#endif

        public bool StartAutomatically { get => startAutomatically; set => startAutomatically = value; }
        internal TestLocationLauncher TestLauncher => GetComponent<TestLocationLauncher>();
        public System.Collections.Generic.IReadOnlyList<string> AdditionalLocationScenes =>
            TestLauncher != null ? System.Array.Empty<string>() : additionalLocationScenes;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ApplicationContext>(Lifetime.Singleton);
            builder.Register<AudioVolumeSettings>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<AudioVolumeSettings>());
            playerMovement.Validate();
            builder.RegisterInstance(playerMovement);
            builder.Register<PlayerInputService>(Lifetime.Singleton).As<IPlayerInput>().As<IInteractionInput>().As<IPauseInput>();
            builder.Register<GameSession>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.RegisterEntryPoint<HouseCheckpointEntryPoint>();
            if (startAutomatically)
                builder.RegisterEntryPoint<BootstrapEntryPoint>().WithParameter(
                    TestLauncher != null ? TestLauncher.ScenePath : initialLocationPath);
        }
    }
}
