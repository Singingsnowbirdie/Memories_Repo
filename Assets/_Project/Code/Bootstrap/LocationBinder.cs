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
        [System.Serializable] private sealed class LegacyFirstAidState
        {
            public bool started = false;
            public string hints = null;
        }
        [System.Serializable] private sealed class LegacyFirstAidHints { public string[] active = null; }

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
            // Apply authored runtime defaults before binding and restoring saved progress.
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                    tracker.ApplyInitialVisibility();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var highway in root.GetComponentsInChildren<Memories.Gameplay.HighwayLights>(true))
                    highway.InitializeControls(scope.Container.Resolve<Memories.Gameplay.UI.GameplayModeState>());
            foreach (var root in scene.GetRootGameObjects())
                foreach (var teleport in root.GetComponentsInChildren<TeleportPlayerReaction>(true))
                    teleport.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<PlayerMovementModel>());
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For(scene);
                bool completed = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>()
                    .Contains("garage_first_memory");
                bindings.StandingGarageFather.SetActive(!completed);
                bindings.KneelingGarageFather.SetActive(completed);
            }
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For(scene);
                bindings.GarageEntrance.enabled = !isCurrentLocation;
                bindings.GarageSideDoor.SetActive(isCurrentLocation);
            }
            if (scene.path == TestLocationLauncher.BackyardPath)
            {
                var bindings = LocationBindings.For(scene);
                bindings.BackyardEntrance.enabled = !isCurrentLocation;
                if (isCurrentLocation)
                    foreach (var group in bindings.BackyardEntranceLighting.GetComponentsInChildren<LightingGroup>(true))
                        group.RestoreBrightness(1);
            }
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
            bool legacyGymDoorOpen = scope.Container.Resolve<ObjectStateStore>()
                .GetSavedPart("school.gym_entrance", "school-gym-entry") == "corridor-open";
            // Новые створки восстанавливаются из собственных состояний; старые сохранения — по завершённому звонку.
            bool courtyardWasOpened = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_call_ambulance");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                {
                    if (isCurrentLocation && !tracker.HasRestoredState &&
                        (tracker.PersistentId == "school.gym_door_closed" || tracker.PersistentId == "school.gym_door_open"))
                        tracker.gameObject.SetActive(legacyGymDoorOpen == (tracker.PersistentId == "school.gym_door_open"));
                    if (tracker.PersistentId.StartsWith("school.environment_") && !tracker.HasRestoredState && isCurrentLocation)
                        tracker.gameObject.SetActive(true);
                    // Older school saves have no separate entrance object states.
                    if ((tracker.PersistentId == "school.garage_corridor" ||
                         tracker.PersistentId == "school.garage_opening_left" ||
                         tracker.PersistentId == "school.garage_opening_right") && !tracker.HasRestoredState && isCurrentLocation)
                        tracker.gameObject.SetActive(false);
                    if ((tracker.PersistentId == "school.ambulance_beacons" ||
                         tracker.PersistentId == "school.yard_animation") && !tracker.HasRestoredState)
                        tracker.gameObject.SetActive(courtyardWasOpened);
                    if (tracker.PersistentId.StartsWith("school.courtyard_") && !tracker.HasRestoredState)
                        tracker.gameObject.SetActive(courtyardWasOpened == tracker.PersistentId.EndsWith("_open"));
                    if (tracker.PersistentId == "school.ambulance" && !courtyardWasOpened)
                        tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
                }
            var schoolTrackers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ObjectStateTracker>(true)).ToArray();
            var giles = schoolTrackers.FirstOrDefault(tracker => tracker.PersistentId == "school.giles");
            if (giles != null)
            {
                var store = scope.Container.Resolve<ObjectStateStore>();
                string legacy = store.GetSavedPart("school.gym_entrance", "school-first-aid");
                if (!string.IsNullOrEmpty(legacy))
                {
                    var state = UnityEngine.JsonUtility.FromJson<LegacyFirstAidState>(legacy);
                    var hints = string.IsNullOrEmpty(state.hints) ? null : UnityEngine.JsonUtility.FromJson<LegacyFirstAidHints>(state.hints);
                    if (state.started && hints?.active != null && System.Array.Exists(hints.active, id =>
                        id == "school.first-aid-kit" || id == "school.ice-pack" || id == "school.ruler" ||
                        id == "school.tissues" || id == "school.return-to-coach"))
                        giles.GetComponent<ReactionInteractable>().RestoreState("{\"active\":[\"school.first-aid-kit\"]}");
                    giles.Capture();
                    store.RemoveSavedPart("school.gym_entrance", "school-first-aid");
                }
            }
            var interiorAnimation = schoolTrackers.FirstOrDefault(tracker => tracker.PersistentId == "school.interior_animation");
            if (interiorAnimation != null && !interiorAnimation.HasRestoredState)
                interiorAnimation.gameObject.SetActive(schoolTrackers.Any(tracker =>
                    tracker.PersistentId == "school.gym_door_open" && tracker.gameObject.activeSelf));
            // До вызова скорой старые сохранения могли скрывать уже ожидающие во дворе машину и врача.
            if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_call_ambulance"))
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                        if ((tracker.PersistentId == "school.ambulance" || tracker.PersistentId == "school.ambulance_doctor") && tracker.HasRestoredState)
                            tracker.gameObject.SetActive(true);
            // В старых сохранениях до появления телефона и монеты интерактивность была отключена отдельно.
            if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_coach_fourth_memory"))
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                        if ((tracker.PersistentId == "school.payphone" || tracker.PersistentId == "school.vending_coin") && tracker.HasRestoredState)
                            tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            // До первого школьного воспоминания Джайлз скрыт, но готов к взаимодействию после появления.
            if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_commentator_memory"))
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                        if (tracker.PersistentId == "school.giles" && tracker.HasRestoredState)
                            tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            // Старые сохранения до появления тренера содержат лишнее отключение интерактивности.
            if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_giles_second_memory"))
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                        if (tracker.PersistentId == "school.coach" && tracker.HasRestoredState)
                            tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            if (scene.path == TestLocationLauncher.GaragePath)
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                        if (tracker.PersistentId == "garage.headlamp" || tracker.PersistentId == "garage.toolbox" ||
                            tracker.PersistentId == "garage.compressor_pickup" || tracker.PersistentId == "garage.vacuum_hose" ||
                            tracker.PersistentId == "garage.silicone_spray" || tracker.PersistentId == "garage.paper_wipes")
                            // Older saves may contain the former disabled interaction state.
                            tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            if (isCurrentLocation && scene.path == TestLocationLauncher.SchoolPath)
                LocationBindings.For(scene).GymEntrance.enabled = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.RestoreInitialVisibility(scope.Container.Resolve<QuestInventory>());
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For(scene);
                var memories = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>();
                var variables = scope.Container.Resolve<CustomVariableStore>();
                bool completed = memories.Contains("garage_second_memory") ||
                    variables.Get(bindings.GarageVariables, "father_compressor_delivered");
                bool laterStage = memories.Contains("garage_third_memory") || memories.Contains("garage_final_memory") ||
                    variables.Get(bindings.GarageVariables, "father_hood_items_delivered") ||
                    variables.Get(bindings.GarageVariables, "father_final_items_delivered");
                bindings.HoodGarageFather.SetActive(completed && !laterStage);
                if (completed || laterStage) bindings.KneelingGarageFather.SetActive(false);
            }
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
            if (scene.path == "Assets/_Project/Locations/Park/Park.unity")
                RestoreParkCinema(LocationBindings.For(scene).ParkCinema,
                    scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<ObjectStateStore>());
            if (scene.path == "Assets/_Project/Locations/Park/Park.unity")
                RestoreParkMimes(LocationBindings.For(scene).ParkMimes,
                    scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<ObjectStateStore>());
            if (scene.path == "Assets/_Project/Locations/Park/Park.unity")
                RestoreParkCafe(LocationBindings.For(scene).ParkCafe,
                    scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<QuestInventory>());
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For(scene);
                bool completed = scope.Container.Resolve<CustomVariableStore>()
                    .Get(bindings.GarageVariables, "father_hood_items_delivered") ||
                    scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("garage_third_memory");
                bindings.WorkingGarageFather.SetActive(completed);
                if (completed)
                {
                    bindings.GarageHood.RestoreState("open");
                    bindings.HoodGarageFather.SetActive(false);
                }
            }
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For(scene);
                var variables = scope.Container.Resolve<CustomVariableStore>();
                bool completed = variables.Get(bindings.GarageVariables, "father_final_items_delivered") ||
                    scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("garage_final_memory");
                bindings.FinalGarageFather.SetActive(completed);
                if (completed)
                {
                    variables.Set(bindings.GarageVariables, "father_final_items_delivered", true);
                    bindings.WorkingGarageFather.SetActive(false);
                    transitions.RestoreGarageSchoolAsync(bindings.SchoolConnection, bindings.GarageGate).Forget();
                }
            }
            if (scene.path == TestLocationLauncher.BackyardPath)
            {
                // Однократное восстановление, включая старые сохранения без флага завершения игры.
                var bindings = LocationBindings.For(scene);
                var variables = scope.Container.Resolve<CustomVariableStore>();
                var inventory = scope.Container.Resolve<QuestInventory>();
                var bank = bindings.BackyardVariables;
                bool completed = variables.Get(bank, "baseball_game_completed") ||
                    scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>()
                        .Contains("backyard_baseball_second_memory");
                if (completed)
                {
                    variables.Set(bank, "baseball_game_completed", true);
                    if (inventory.Contains(bindings.BaseballBat.Id)) inventory.TryConsume(bindings.BaseballBat);
                }
                bool ready = !completed && variables.Get(bank, "ben_received_cap") &&
                    variables.Get(bank, "ben_received_baseball") && variables.Get(bank, "baseball_bat_found");
                bindings.IntactBaseballWindow.SetActive(!completed);
                bindings.BrokenBaseballWindow.SetActive(completed);
                if (completed) bindings.BenPitcher.SetActive(false);
                bindings.SeatedBen.SetActive(completed);
                // В старых снимках интерактивность этого объекта могла быть выключена.
                bindings.BaseballAreaReady.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
                bindings.BaseballAreaNormal.SetActive(!ready);
                bindings.BaseballAreaReady.SetActive(ready);
            }
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
        private static void RestoreParkCafe(LocationBindings.ParkCafeBindings bindings,
            CustomVariableStore state, QuestInventory inventory)
        {
            bool arrived = bindings.lily.gameObject.activeSelf;
            bool bought = state.Get(bindings.variables, "ordered") || inventory.WasCollected(bindings.receipt.Id);
            bool coffeeRead = state.Get(bindings.variables, "coffee_read");
            bool napkinRead = state.Get(bindings.variables, "napkin_read");
            bool complete = state.Get(bindings.variables, "completed") || (coffeeRead && napkinRead);
            if (bought) state.Set(bindings.variables, "ordered", true);
            if (complete) state.Set(bindings.variables, "completed", true);
            bindings.lily.gameObject.SetActive(arrived && !bought);
            // Hidden objects stay ready for their explicit reveal graph; progress controls interaction.
            bindings.lily.SetInteractionEnabled(!bought && !complete && !state.Get(bindings.variables, "unlocked"));
            bindings.truck.SetInteractionEnabled(!bought && !complete && state.Get(bindings.variables, "unlocked"));
            bindings.order.SetActive(bought && !complete);
            bindings.coffeeOne.SetInteractionEnabled(!complete && !coffeeRead);
            bindings.coffeeTwo.SetInteractionEnabled(!complete && !coffeeRead);
            bindings.napkin.SetInteractionEnabled(!complete && !napkinRead);
            bindings.lighting.RestoreBrightness((arrived || bought) && !complete ? 1 : 0);
            if (bindings.lakeLighting != null) bindings.lakeLighting.RestoreBrightness(complete ? 1 : 0);
            if (bindings.dockLily != null && bindings.dockBoat != null && bindings.dockVariables != null)
            {
                bool talked = state.Get(bindings.dockVariables, "lily_spoken");
                bindings.dockLily.gameObject.SetActive(complete);
                bindings.dockLily.SetInteractionEnabled(!talked);
                bindings.dockBoat.SetInteractionEnabled(complete && talked);
            }
        }
        private static void RestoreParkMimes(LocationBindings.ParkMimeBindings bindings,
            QuestInventory inventory, ObjectStateStore states)
        {
            int.TryParse(states.GetSavedPart("park.mimes.sequence", "park.mimes.sequence"), out int legacyStage);
            bool bought = inventory.WasCollected(bindings.pendants.Id);
            bool finished = legacyStage >= 4 || inventory.IsConsumed(bindings.pendants.Id);
            bool arrived = legacyStage > 0 || inventory.IsConsumed(bindings.tickets.Id);
            // Also completes restoration when a save was made during the preceding dissolve.
            bindings.lily.SetActive(arrived && !finished);
            bindings.lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(!finished);
            if (!bindings.suitcase.GetComponent<ObjectStateTracker>().HasRestoredState || bought || finished)
                bindings.suitcase.SetInteractionEnabled(legacyStage == 2 && !bought && !finished);
            bindings.lighting.RestoreBrightness(arrived && !finished ? 1 : 0);
            if (bindings.cafeLighting) bindings.cafeLighting.RestoreBrightness(finished ? 1 : 0);
            if (bindings.cafeLily) bindings.cafeLily.SetActive(finished);
        }

        private static void RestoreParkCinema(LocationBindings.ParkCinemaBindings bindings,
            QuestInventory inventory, ObjectStateStore states)
        {
            int.TryParse(states.GetSavedPart("park.lily.bouquet", "park.cinema.sequence"), out int legacyStage);
            // Migrate the former custom reaction once, without replaying gameplay.
            bool bouquetDelivered = states.GetSavedPart("park.lily.bouquet", "park.lily.bouquet") == "1" ||
                inventory.IsConsumed(bindings.bouquetItem.Id);
            if (bouquetDelivered)
            {
                bindings.entranceLily.GetComponent<ReactionInteractable>().SetInteractionEnabled(false);
                bindings.entranceLily.SetActive(false);
                bindings.approachLighting.RestoreBrightness(0);
            }
            bool bought = inventory.WasCollected(bindings.tickets.Id);
            bool finished = legacyStage >= 4 || inventory.IsConsumed(bindings.tickets.Id);
            bool arrived = legacyStage > 0 || inventory.IsConsumed(bindings.bouquetItem.Id) || bought;
            if (!bindings.lily.GetComponent<ObjectStateTracker>().HasRestoredState || finished)
            {
                bindings.lily.SetActive(arrived && !finished);
            }
            if (!bindings.cashier.GetComponent<ObjectStateTracker>().HasRestoredState || finished)
            {
                bool revealed = legacyStage >= 2 || bought;
                bindings.cashier.SetActive(revealed && !finished);
            }
            // Hidden encounters are ready for their reveal, including old object snapshots.
            bindings.lily.GetComponent<ReactionInteractable>().SetInteractionEnabled(!finished);
            bindings.cashier.GetComponent<ReactionInteractable>().SetInteractionEnabled(!bought && !finished);
            bindings.cinemaLighting.RestoreBrightness(arrived && !finished ? 1 : 0);
            bindings.benchLighting.RestoreBrightness(finished ? 0 : 1);
        }
    }
}
