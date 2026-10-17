using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class AppLifetimeScope : LifetimeScope
    {
        public const string DefaultLocationPath = "Assets/Scenes/FirstPersonTest/FirstPersonTest.unity";
        [SerializeField] private string initialLocationPath = DefaultLocationPath;
        [SerializeField] private bool startAutomatically = true;

        public bool StartAutomatically { get => startAutomatically; set => startAutomatically = value; }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ApplicationContext>(Lifetime.Singleton);
            builder.Register<GameSession>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            if (startAutomatically)
                builder.RegisterEntryPoint<BootstrapEntryPoint>().WithParameter(initialLocationPath);
        }
    }
}
