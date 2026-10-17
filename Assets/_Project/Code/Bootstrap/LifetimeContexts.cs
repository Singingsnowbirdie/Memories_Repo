using System;
using System.Threading;

namespace Memories.Bootstrap
{
    public abstract class LifetimeContext : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        public Guid Id { get; } = Guid.NewGuid();
        public CancellationToken Token { get; }
        public bool IsDisposed { get; private set; }

        protected LifetimeContext() => Token = cancellation.Token;

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            try { cancellation.Cancel(); }
            finally { cancellation.Dispose(); }
        }
    }

    public sealed class ApplicationContext : LifetimeContext { }
    public sealed class SessionContext : LifetimeContext { }
    public sealed class LocationContext : LifetimeContext { }
}
