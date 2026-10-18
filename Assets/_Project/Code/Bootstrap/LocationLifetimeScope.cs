using VContainer;
using VContainer.Unity;
using Memories.Gameplay.Player;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Triggers;

namespace Memories.Bootstrap
{
    public sealed class LocationLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LocationContext>(Lifetime.Singleton);
            builder.Register<GameplayModeState>(Lifetime.Singleton);
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
            builder.Register<IStoryTextView>(container =>
            {
                var window = new GameObject("Story text", typeof(RectTransform));
                window.transform.SetParent(transform, false);
                return window.AddComponent<StoryTextView>();
            }, Lifetime.Singleton);
            builder.Register<StoryTextPresenter>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<IPauseView>(container =>
            {
                if (EventSystem.current == null)
                {
                    var events = new GameObject("UI EventSystem");
                    events.transform.SetParent(transform, false);
                    events.AddComponent<EventSystem>();
                    events.AddComponent<InputSystemUIInputModule>();
                }
                var menu = new GameObject("Pause menu", typeof(RectTransform));
                menu.transform.SetParent(transform, false);
                return menu.AddComponent<PauseMenuView>();
            }, Lifetime.Singleton);
            builder.Register<PausePresenter>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.RegisterBuildCallback(container => container.Resolve<PausePresenter>());
            builder.Register<IMemoryView>(container =>
            {
                var window = new GameObject("Memory window", typeof(RectTransform));
                window.transform.SetParent(transform, false);
                return window.AddComponent<MemoryView>();
            }, Lifetime.Singleton);
            builder.Register<MemoryPresenter>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<IMemorySound>(container =>
            {
                var audio = new GameObject("Memory sounds");
                audio.transform.SetParent(transform, false);
                var player = audio.AddComponent<MemorySoundPlayer>();
                player.Initialize(container.Resolve<AudioVolumeSettings>());
                return player;
            }, Lifetime.Singleton);
            builder.Register<IMemoryMusic>(container =>
            {
                var audio = new GameObject("Memory music");
                audio.transform.SetParent(transform, false);
                var player = audio.AddComponent<MemoryMusicPlayer>();
                player.Initialize(container.Resolve<AudioVolumeSettings>());
                return player;
            }, Lifetime.Singleton);
            builder.RegisterBuildCallback(container =>
            {
                // Register Escape handling after pause: reading consumes it by closing its own window.
                container.Resolve<PausePresenter>();
                var presenter = container.Resolve<MemoryPresenter>();
                var collection = container.Resolve<MemoryCollection>();
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (var shard in root.GetComponentsInChildren<MemoryShard>(true))
                        shard.Initialize(presenter, collection);
                    foreach (var triggerEvent in root.GetComponentsInChildren<TriggerEvent>(true))
                        triggerEvent.Initialize(container.Resolve<StoryTextPresenter>(), presenter);
                }
            });
        }
    }
}
