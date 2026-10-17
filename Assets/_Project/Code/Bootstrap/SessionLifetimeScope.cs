using VContainer;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class SessionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SessionContext>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<SessionContext>());
        }
    }
}
