using VContainer;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class LocationLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LocationContext>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<LocationContext>());
        }
    }
}
