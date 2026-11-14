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
                        () => locations.ChangeLocationAsync("Assets/_Project/Locations/Park/Park.unity"));
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
            foreach (var root in scene.GetRootGameObjects())
                foreach (var entrance in root.GetComponentsInChildren<BackyardEntranceReaction>(true))
                    entrance.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>(),
                        () => transitions.EnterConnectedSceneAsync(scene), isCurrentLocation);
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
                foreach (var reaction in root.GetComponentsInChildren<CloseEntranceDoorsReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<CloseStairDoorReaction>(true))
                    reaction.Restore(scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.TriggerEvent>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.SceneTrigger>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), locations,
                        scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<PlayerLookAction>());
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>(), locations, scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<PlayerLookAction>());
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var door in root.GetComponentsInChildren<OpenCourtyardDoorReaction>(true))
                    door.Initialize(
                        path => transitions.LoadConnectedSceneAsync(path, door),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
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
            foreach (var root in scene.GetRootGameObjects())
                foreach (var cinema in root.GetComponentsInChildren<ParkCinemaSequence>(true))
                    cinema.Bind(scope.Container.Resolve<QuestInventory>());
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
                foreach (var reaction in root.GetComponentsInChildren<RemoveClockBatteryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<RemoveToyPlaneBatteryReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var reaction in root.GetComponentsInChildren<RefreshBaseballAreaReaction>(true))
                    reaction.Initialize(scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<CustomVariableStore>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
        }
    }
}
