using System.Linq;
using Cysharp.Threading.Tasks;
using Memories.Contracts;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Player;
using Memories.Gameplay.Reactions;
using UnityEngine.SceneManagement;
using VContainer;

namespace Memories.Bootstrap
{
    // Подключает компоненты загруженной локации и восстанавливает их состояние в установленном порядке.
    internal sealed class LocationBinder
    {
        private readonly LocationTransitions transitions;
        private readonly ILocationTransition locations;

        public LocationBinder(LocationTransitions transitions, ILocationTransition locations)
        {
            this.transitions = transitions;
            this.locations = locations;
        }
        public void Bind(Scene scene, LocationLifetimeScope scope, bool isCurrentLocation,
            TesterCheckpoint? testerCheckpoint, TesterCheckpointCatalog checkpointCatalog)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var departure in root.GetComponentsInChildren<SchoolAmbulanceDepartureReaction>(true))
                    departure.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(),
                        () => locations.ChangeLocationAsync("Assets/_Project/Locations/Park/Park.unity"),
                        scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<PlayerMovementModel>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var phone in root.GetComponentsInChildren<RevealSchoolPhoneReaction>(true))
                    phone.Initialize(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var supplies in root.GetComponentsInChildren<SchoolFirstAidProgressReaction>(true))
                    supplies.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var connection in root.GetComponentsInChildren<GarageSchoolConnection>(true))
                    connection.Initialize(transitions.LoadGarageSchoolAsync);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<SchoolGymEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(), () => transitions.EnterConnectedSceneAsync(scene));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageFatherMemoryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<GarageEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        () => transitions.EnterGarageAsync(scene), isCurrentLocation);
            if (scene.path == TestLocationLauncher.BackyardPath)
            {
                var bindings = LocationBindings.For(scene);
                bindings.BackyardEntrance.enabled = !isCurrentLocation;
                if (isCurrentLocation)
                    foreach (var group in bindings.BackyardEntranceLighting.GetComponentsInChildren<LightingGroup>(true))
                        group.RestoreBrightness(1);
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var attachment in root.GetComponentsInChildren<BackyardGarageAttachment>(true))
                    attachment.Initialize(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>(), transitions.LoadBackyardGarageAsync);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var photo in root.GetComponentsInChildren<RevealPhotoReaction>(true))
                    photo.Restore(scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        scope.Container.Resolve<PlayerMotor>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var focus in root.GetComponentsInChildren<FocusCameraReaction>(true))
                    focus.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>());
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.TriggerEvent>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.SceneTrigger>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), locations,
                        scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<PlayerLookAction>(), scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>());
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), locations, scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<PlayerLookAction>(), scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>());
            }
            HintProgressRestoration.Restore(scope.Container.Resolve<QuestInventory>(),
                scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            scope.Container.Resolve<ObjectStateStore>().RestoreScene(scene);
            if (isCurrentLocation)
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var entrance in root.GetComponentsInChildren<SchoolGymEntranceReaction>(true))
                        entrance.RestoreForDirectLoad();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.RestoreInitialVisibility(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageHoodFatherMemoryReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var visibility in root.GetComponentsInChildren<SavedObjectVisibility>(true))
                    visibility.Restore(scope.Container.Resolve<CustomVariableStore>());
            if (testerCheckpoint.HasValue) checkpointCatalog.ApplyScene(testerCheckpoint.Value, scene);
            // One-time restoration also covers old saves and tester checkpoints without object snapshots.
            if (scene.path == "Assets/_Project/Locations/House/House.unity")
            {
                bool doorsClosed = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>()
                    .Contains("house-tv-baseball");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                        switch (tracker.PersistentId)
                        {
                            case "house.entrance.open":
                            case "house.stair.open":
                                if (!tracker.HasRestoredState || testerCheckpoint.HasValue)
                                    tracker.gameObject.SetActive(!doorsClosed);
                                break;
                            case "house.entrance.closed":
                            case "house.stair.closed":
                                if (!tracker.HasRestoredState || testerCheckpoint.HasValue)
                                    tracker.gameObject.SetActive(doorsClosed);
                                break;
                        }
            }
            if (scene.path == "Assets/_Project/Locations/House/House.unity")
            {
                var trackers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ObjectStateTracker>(true)).ToArray();
                bool courtyardOpen = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>()
                    .Contains("house-tv-baseball") || trackers.Any(t =>
                        t.PersistentId == "house.courtyard.open" && t.HasRestoredState && t.gameObject.activeSelf);
                if (courtyardOpen)
                    RestoreCourtyardAsync(LocationBindings.For(scene).CourtyardConnection, trackers).Forget();
            }
            // House lighting is initialized once; completion of the highway animation runs its graph.
            if (scene.path == "Assets/_Project/Locations/House/House.unity")
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var group in root.GetComponentsInChildren<LightingGroup>(true))
                    {
                        var highway = UnityEngine.Object.FindFirstObjectByType<Memories.Gameplay.HighwayLights>();
                        if (highway != null) highway.RegisterHouse(group);
                        else group.RestoreBrightness(1);
                    }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var cinema in root.GetComponentsInChildren<ParkCinemaReaction>(true))
                    cinema.RestoreAfterLoad(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var mimes in root.GetComponentsInChildren<ParkMimeReaction>(true))
                    mimes.RestoreAfterLoad(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var cafe in root.GetComponentsInChildren<ParkCafeDissolveReaction>(true))
                    cafe.RestoreAfterLoad(scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageOpenHoodFatherReaction>(true))
                    reaction.Restore(scope.Container.Resolve<CustomVariableStore>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<GarageFinalMemoryReaction>(true))
                    reaction.Restore(scope.Container.Resolve<QuestInventory>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<ShowSeatedBenReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<RefreshBaseballAreaReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
        }

        private async UniTask RestoreCourtyardAsync(UnityEngine.Transform courtyard, ObjectStateTracker[] trackers)
        {
            // Restore the saved presentation once; do not run gameplay graphs while loading.
            await UniTask.NextFrame();
            if (courtyard == null) return;
            await locations.LoadSegmentAsync(TestLocationLauncher.BackyardPath, courtyard, "Вход из дома");
            if (courtyard == null) return;
            foreach (var tracker in trackers)
            {
                if (tracker == null) continue;
                if (tracker.PersistentId == "house.courtyard.open") tracker.gameObject.SetActive(true);
                else if (tracker.PersistentId == "house.courtyard.closed" ||
                    tracker.PersistentId.StartsWith("house.courtyard.cover.", System.StringComparison.Ordinal))
                    tracker.gameObject.SetActive(false);
            }
        }
    }
}
