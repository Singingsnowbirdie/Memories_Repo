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
                foreach (var teleport in root.GetComponentsInChildren<TeleportPlayerReaction>(true))
                    teleport.Initialize(scope.Container.Resolve<PlayerMotor>(), scope.Container.Resolve<PlayerMovementModel>());
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For<GarageBindings>(scene);
                bindings.GarageEntrance.enabled = !isCurrentLocation;
                bindings.GarageSideDoor.SetActive(isCurrentLocation);
            }
            if (scene.path == TestLocationLauncher.BackyardPath)
            {
                var bindings = LocationBindings.For<BackyardBindings>(scene);
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
                foreach (var graphBinding in root.GetComponentsInChildren<ReactionGraphBinding>(true))
                    graphBinding.Initialize(scope.Container.Resolve<ReactionManager>());
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.TriggerEvent>(true))
                    trigger.Initialize(scope.Container.Resolve<Memories.Gameplay.UI.StoryTextPresenter>(),
                        scope.Container.Resolve<Memories.Gameplay.Memories.MemoryPresenter>());
                foreach (var trigger in root.GetComponentsInChildren<Memories.Gameplay.Triggers.SceneTrigger>(true))
                    trigger.Initialize(scope.Container.Resolve<ReactionManager>());
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.Initialize(scope.Container.Resolve<ReactionManager>(), scope.Container.Resolve<QuestInventory>());
            }
            HintProgressRestoration.Restore(scope.Container.Resolve<QuestInventory>(),
                scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            scope.Container.Resolve<ObjectStateStore>().RestoreScene(scene);
            if (scene.path == TestLocationLauncher.SchoolPath)
            {
                var school = LocationBindings.For<SchoolBindings>(scene);
                bool legacyGymDoorOpen = scope.Container.Resolve<ObjectStateStore>()
                    .GetSavedPart("school.gym_entrance", "school-gym-entry") == "corridor-open";
                // Новые створки восстанавливаются из собственных состояний; старые сохранения — по завершённому звонку.
                bool courtyardWasOpened = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_call_ambulance");
                void RestoreDefault(ObjectStateTracker tracker, bool active)
                {
                    if (!tracker.HasRestoredState) tracker.gameObject.SetActive(active);
                }
                if (isCurrentLocation)
                {
                    RestoreDefault(school.GymDoorClosed, !legacyGymDoorOpen);
                    RestoreDefault(school.GymDoorOpen, legacyGymDoorOpen);
                    RestoreDefault(school.EnvironmentCorridor, true);
                    RestoreDefault(school.EnvironmentLockerRooms, true);
                    RestoreDefault(school.GarageCorridor, false);
                    RestoreDefault(school.GarageOpeningLeft, false);
                    RestoreDefault(school.GarageOpeningRight, false);
                }
                RestoreDefault(school.AmbulanceBeacons, courtyardWasOpened);
                RestoreDefault(school.YardAnimation, courtyardWasOpened);
                RestoreDefault(school.CourtyardLeftOpen, courtyardWasOpened);
                RestoreDefault(school.CourtyardRightOpen, courtyardWasOpened);
                RestoreDefault(school.CourtyardLeftClosed, !courtyardWasOpened);
                RestoreDefault(school.CourtyardRightClosed, !courtyardWasOpened);
                if (!courtyardWasOpened)
                    school.Ambulance.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
                var giles = school.Giles;
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
                var interiorAnimation = school.InteriorAnimation;
                if (interiorAnimation != null && !interiorAnimation.HasRestoredState)
                    interiorAnimation.gameObject.SetActive(school.GymDoorOpen.gameObject.activeSelf);
                // До вызова скорой старые сохранения могли скрывать уже ожидающие во дворе машину и врача.
                if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_call_ambulance"))
                    foreach (var tracker in new[] { school.Ambulance, school.AmbulanceDoctor })
                        if (tracker.HasRestoredState) tracker.gameObject.SetActive(true);
                // В старых сохранениях до появления телефона и монеты интерактивность была отключена отдельно.
                if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_coach_fourth_memory"))
                    foreach (var tracker in new[] { school.Payphone, school.VendingCoin })
                        if (tracker.HasRestoredState) tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
                // До первого школьного воспоминания Джайлз скрыт, но готов к взаимодействию после появления.
                if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_commentator_memory") && school.Giles.HasRestoredState)
                    school.Giles.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
                // Старые сохранения до появления тренера содержат лишнее отключение интерактивности.
                if (!scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>().Contains("school_giles_second_memory") && school.Coach.HasRestoredState)
                    school.Coach.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            }
            if (scene.path == TestLocationLauncher.GaragePath)
                foreach (var tracker in LocationBindings.For<GarageBindings>(scene).PickupItems)
                    tracker.GetComponent<ReactionInteractable>().SetInteractionEnabled(true);
            if (isCurrentLocation && scene.path == TestLocationLauncher.SchoolPath)
                LocationBindings.For<SchoolBindings>(scene).GymEntrance.enabled = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var interactable in root.GetComponentsInChildren<ReactionInteractable>(true))
                    interactable.RestoreInitialVisibility(scope.Container.Resolve<QuestInventory>());
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For<GarageBindings>(scene);
                var memories = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>();
                var variables = scope.Container.Resolve<CustomVariableStore>();
                bool completed = memories.Contains("garage_second_memory") ||
                    variables.Get(bindings.GarageVariables, "father_compressor_delivered");
                bool laterStage = memories.Contains("garage_third_memory") || memories.Contains("garage_final_memory") ||
                    variables.Get(bindings.GarageVariables, "father_hood_items_delivered") ||
                    variables.Get(bindings.GarageVariables, "father_final_items_delivered");
                // Restore progression after object snapshots so stale visibility cannot bring back the first father.
                bool firstMemoryCompleted = memories.Contains("garage_first_memory");
                bindings.StandingGarageFather.SetActive(!firstMemoryCompleted && !completed && !laterStage);
                bindings.KneelingGarageFather.SetActive(firstMemoryCompleted && !completed && !laterStage);
                bindings.HoodGarageFather.SetActive(completed && !laterStage);
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var visibility in root.GetComponentsInChildren<SavedObjectVisibility>(true))
                    visibility.Restore(scope.Container.Resolve<CustomVariableStore>());
            if (testerCheckpoint.HasValue) checkpointCatalog.ApplyScene(testerCheckpoint.Value, scene);
            // One-time restoration also covers old saves and tester checkpoints without object snapshots.
            if (scene.path == "Assets/_Project/Locations/House.unity")
            {
                bool doorsClosed = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>()
                    .Contains("house-tv-baseball");
                var house = LocationBindings.For<HouseBindings>(scene);
                foreach (var tracker in new[] { house.EntranceOpen, house.StairOpen })
                    if (!tracker.HasRestoredState || testerCheckpoint.HasValue)
                        tracker.gameObject.SetActive(!doorsClosed);
                foreach (var tracker in new[] { house.EntranceClosed, house.StairClosed })
                    if (!tracker.HasRestoredState || testerCheckpoint.HasValue)
                        tracker.gameObject.SetActive(doorsClosed);
            }
            if (scene.path == TestLocationLauncher.HousePath)
            {
                var house = LocationBindings.For<HouseBindings>(scene);
                bool courtyardOpen = scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>()
                    .Contains("house-tv-baseball");
                house.CourtyardOpen.gameObject.SetActive(false);
                house.CourtyardClosed.gameObject.SetActive(true);
                house.CourtyardPlaceholders.gameObject.SetActive(true);
                if (courtyardOpen) RestoreCourtyardAsync(house).Forget();
            }
            RestoreHighwayLighting(scene, scope.Container.Resolve<CustomVariableStore>());
            // Direct starts outside the highway skip its sky reveal.
            if (isCurrentLocation && scene.path != TestLocationLauncher.HighwayPath)
                UnityEngine.Object.FindFirstObjectByType<Memories.Gameplay.GhostCosmosSky>()?.ShowImmediately();
            // Starting directly in the house (save/checkpoint) skips the highway introduction.
            if (scene.path == TestLocationLauncher.HousePath && isCurrentLocation)
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var group in root.GetComponentsInChildren<LightingGroup>(true))
                        group.RestoreBrightness(1);
            // Legacy saves could deactivate the graph binding together with the house light.
            // Restore the group's brightness first, then keep its graph available for explicit calls.
            if (scene.path == TestLocationLauncher.HousePath)
            {
                var highway = SceneManager.GetSceneByPath(TestLocationLauncher.HighwayPath);
                bool complete = isCurrentLocation || (highway.IsValid() && highway.isLoaded &&
                    scope.Container.Resolve<CustomVariableStore>()
                        .Get(LocationBindings.For<HighwayBindings>(highway).HighwayVariables, "lighting_complete"));
                var tracker = LocationBindings.For<HouseBindings>(scene).Lighting;
                tracker.GetComponent<LightingGroup>().RestoreBrightness(complete ? 1 : 0);
                tracker.gameObject.SetActive(true);
            }
            if (scene.path == "Assets/_Project/Locations/Park.unity")
                RestoreParkCinema(LocationBindings.For<ParkBindings>(scene).ParkCinema,
                    scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<ObjectStateStore>());
            if (scene.path == "Assets/_Project/Locations/Park.unity")
                RestoreParkMimes(LocationBindings.For<ParkBindings>(scene).ParkMimes,
                    scope.Container.Resolve<QuestInventory>(), scope.Container.Resolve<ObjectStateStore>());
            if (scene.path == "Assets/_Project/Locations/Park.unity")
                RestoreParkCafe(LocationBindings.For<ParkBindings>(scene).ParkCafe,
                    scope.Container.Resolve<CustomVariableStore>(), scope.Container.Resolve<QuestInventory>(),
                    scope.Container.Resolve<Memories.Gameplay.Memories.MemoryCollection>());
            if (scene.path == TestLocationLauncher.GaragePath)
            {
                var bindings = LocationBindings.For<GarageBindings>(scene);
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
                var bindings = LocationBindings.For<GarageBindings>(scene);
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
                var bindings = LocationBindings.For<BackyardBindings>(scene);
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

        private async UniTask RestoreCourtyardAsync(HouseBindings house)
        {
            await UniTask.NextFrame();
            if (house == null) return;
            await locations.LoadSegmentAsync(TestLocationLauncher.BackyardPath, house.CourtyardConnection, "Вход из дома");
            if (house == null) return;
            house.CourtyardOpen.gameObject.SetActive(true);
            house.CourtyardClosed.gameObject.SetActive(false);
            house.CourtyardPlaceholders.gameObject.SetActive(false);
            foreach (var cover in house.CourtyardCovers)
                if (cover != null) cover.gameObject.SetActive(false);
        }
        private static void RestoreParkCafe(ParkBindings.ParkCafeBindings bindings,
            CustomVariableStore state, QuestInventory inventory, Memories.Gameplay.Memories.MemoryCollection memories)
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
            bool gazeboReached = state.Get(bindings.dockVariables, "gazebo_reached") ||
                memories.Contains("park-boat-conversation");
            bool exitReached = state.Get(bindings.dockVariables, "exit_reached") ||
                memories.Contains("park-gazebo-conversation");
            bool bouquetReturned = state.Get(bindings.dockVariables, "bouquet_returned") ||
                memories.Contains("park-exit-conversation");
            bool bouquetDiscarded = state.Get(bindings.dockVariables, "bouquet_discarded") ||
                (bindings.returnedBouquet != null && inventory.IsConsumed(bindings.returnedBouquet.Id));
            exitReached |= bouquetReturned || bouquetDiscarded;
            gazeboReached |= exitReached;
            if (exitReached) state.Set(bindings.dockVariables, "exit_reached", true);
            if (bouquetReturned) state.Set(bindings.dockVariables, "bouquet_returned", true);
            if (bouquetDiscarded) state.Set(bindings.dockVariables, "bouquet_discarded", true);
            if (gazeboReached) state.Set(bindings.dockVariables, "gazebo_reached", true);
            if (bindings.lakeLighting != null) bindings.lakeLighting.RestoreBrightness(complete && !gazeboReached ? 1 : 0);
            if (bindings.gazeboLighting != null) bindings.gazeboLighting.RestoreBrightness(gazeboReached && !exitReached ? 1 : 0);
            if (bindings.gazeboLily != null)
            {
                bindings.gazeboLily.gameObject.SetActive(gazeboReached && !exitReached);
                bindings.gazeboLily.SetInteractionEnabled(true);
            }
            if (bindings.exitLighting != null) bindings.exitLighting.RestoreBrightness(exitReached ? 1 : 0);
            if (bindings.exitLily != null)
            {
                bindings.exitLily.gameObject.SetActive(exitReached && !bouquetReturned && !bouquetDiscarded);
                bindings.exitLily.SetInteractionEnabled(!bouquetReturned && !bouquetDiscarded);
            }
            if (bindings.exitBin != null) bindings.exitBin.SetInteractionEnabled(bouquetReturned && !bouquetDiscarded);
            if (bindings.returnedBouquet != null)
            {
                if (bouquetDiscarded) inventory.TryConsume(bindings.returnedBouquet);
                else if (bouquetReturned) inventory.TryAdd(bindings.returnedBouquet);
            }
            if (bindings.discardedBouquet != null) bindings.discardedBouquet.SetActive(bouquetDiscarded);
            if (bindings.exitGate != null) bindings.exitGate.RestoreOpen(bouquetDiscarded);
            if (bindings.dockLily != null && bindings.dockBoat != null && bindings.dockVariables != null)
            {
                bool talked = state.Get(bindings.dockVariables, "lily_spoken");
                bindings.dockLily.gameObject.SetActive(complete && !gazeboReached);
                bindings.dockLily.SetInteractionEnabled(!talked && !gazeboReached);
                bindings.dockBoat.SetInteractionEnabled(complete && talked && !gazeboReached);
            }
        }
        private static void RestoreParkMimes(ParkBindings.ParkMimeBindings bindings,
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

        private static void RestoreParkCinema(ParkBindings.ParkCinemaBindings bindings,
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

        private static void RestoreHighwayLighting(Scene scene, CustomVariableStore variables)
        {
            // Restore completed presentation once; live lighting is invoked only by graph nodes.
            if (scene.path == TestLocationLauncher.HighwayPath)
            {
                var bindings = LocationBindings.For<HighwayBindings>(scene);
                bool complete = variables.Get(bindings.HighwayVariables, "lighting_complete");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var group in root.GetComponentsInChildren<LightingGroup>(true))
                        group.RestoreBrightness(complete ? 1 : 0);
                var sky = UnityEngine.Object.FindFirstObjectByType<Memories.Gameplay.GhostCosmosSky>();
                if (sky != null)
                {
                    if (complete) sky.ShowImmediately();
                    else sky.Prepare();
                }
            }
        }
    }
}
