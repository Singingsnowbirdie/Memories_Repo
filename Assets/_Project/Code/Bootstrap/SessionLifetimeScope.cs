using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VContainer;
using VContainer.Unity;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Interaction;

namespace Memories.Bootstrap
{
    public sealed class SessionLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            bool persistProgress = ((AppLifetimeScope)parentReference.Object).PersistProgress;
            builder.Register<SessionContext>(Lifetime.Singleton);
            builder.Register<MemoryCollection>(_ => new MemoryCollection(MemoryCatalog.LoadEntries(), persistProgress), Lifetime.Singleton);
            builder.Register<QuestInventory>(Lifetime.Singleton);
            builder.Register<CustomVariableStore>(_ => new CustomVariableStore(persistProgress: persistProgress), Lifetime.Singleton);
            builder.Register<QuestInventorySave>(container => new QuestInventorySave(
                container.Resolve<QuestInventory>(), QuestItemCatalog.LoadItems(), persistProgress: persistProgress), Lifetime.Singleton)
                .AsSelf().AsImplementedInterfaces();
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<SessionContext>();
                container.Resolve<QuestInventorySave>();
                // UI input must outlive overlapping location loads and unloads.
                if (EventSystem.current == null)
                {
                    var events = new GameObject("UI EventSystem");
                    events.transform.SetParent(transform, false);
                    events.AddComponent<EventSystem>();
                    events.AddComponent<InputSystemUIInputModule>();
                }
            });
        }
    }
}
