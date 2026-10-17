using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Memories.Bootstrap
{
    public sealed class BootstrapEntryPoint : IStartable
    {
        private readonly GameSession session;
        private readonly ApplicationContext application;
        private readonly string locationPath;

        public BootstrapEntryPoint(GameSession session, ApplicationContext application, string locationPath)
        {
            this.session = session;
            this.application = application;
            this.locationPath = locationPath;
        }

        public void Start() => session.StartAsync(locationPath, application.Token).Forget(Debug.LogException);
    }
}
