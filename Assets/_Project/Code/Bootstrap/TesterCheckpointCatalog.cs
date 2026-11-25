using System;
using System.Linq;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    public enum TesterCheckpoint { HouseEntrance, HouseMemoryComplete, BackyardMemoryComplete, GarageCorridorOpen, SchoolCourtyardOpen, ParkExitOpen }

    public sealed class TesterCheckpointCatalog : ScriptableObject
    {
        public CustomVariableBank houseVariables, backyardVariables;
        public MemoryGraph houseMemory, backyardFirstMemory, backyardSecondMemory;
        public QuestItem[] houseItems, backyardItems;
        public CustomVariableBank garageVariables;
        public MemoryGraph[] garageMemories;
        public QuestItem[] garageItems;
        public CustomVariableBank schoolVariables;
        public MemoryGraph[] schoolMemories;
        public QuestItem[] schoolItems;
        public CustomVariableBank parkVariables, parkBouquetVariables, parkMimesVariables, parkCafeVariables, parkDockVariables;
        public QuestItem[] parkConsumedItems;
        public QuestItem parkReceipt;
        public MemoryGraph[] parkMemories;

        public void Seed(TesterCheckpoint checkpoint, CustomVariableStore variables, QuestInventory inventory, MemoryCollection memories)
        {
            if (checkpoint == TesterCheckpoint.HouseEntrance) return;
            variables.Set(houseVariables, "a658183a452d4c2b9ef92252e09264ec", true);
            variables.Set(houseVariables, "woman_missing_remote_read", true);
            variables.Set(houseVariables, "television_on", true);
            foreach (string id in new[] { "remote_found", "remote_battery_first_found", "remote_battery_second_found" })
                variables.Set(houseVariables, id, true);
            foreach (var item in houseItems) inventory.RestoreConsumed(item.Id);
            memories.Collect("house-tv-baseball", houseMemory);
            if (checkpoint != TesterCheckpoint.BackyardMemoryComplete && checkpoint != TesterCheckpoint.GarageCorridorOpen && checkpoint != TesterCheckpoint.SchoolCourtyardOpen && checkpoint != TesterCheckpoint.ParkExitOpen) return;
            foreach (string id in new [] {"ben_alex_inscription_read", "ben_home_first_interaction", "optional_interactives_memory_started",
                "fishing_rods_inspected", "hose_inspected", "bicycle_inspected", "waterguns_inspected", "briefcase_inspected", "wheelbarrow_inspected",
                "baseball_equipment_revealed", "ben_received_baseball", "ben_received_cap", "baseball_bat_found", "baseball_ball_found", "baseball_cap_found"})
                variables.Set(backyardVariables, id, true);
            variables.Set(backyardVariables, "optional_interactives_inspected", 6);
            foreach (var item in backyardItems) inventory.RestoreConsumed(item.Id);
            memories.Collect("backyard_optional_memory", backyardFirstMemory);
            memories.Collect("backyard_baseball_second_memory", backyardSecondMemory);
            variables.Set(backyardVariables, "baseball_game_completed", true);
            if (checkpoint != TesterCheckpoint.GarageCorridorOpen && checkpoint != TesterCheckpoint.SchoolCourtyardOpen && checkpoint != TesterCheckpoint.ParkExitOpen) return;
            foreach (string id in new[] { "father_compressor_requested", "father_hood_items_requested", "father_hood_items_delivered",
                "father_compressor_delivered", "father_final_items_delivered", "compressor_found", "headlamp_found", "toolbox_found",
                "vacuum_hose_found", "silicone_spray_found", "paper_wipes_found" })
                variables.Set(garageVariables, id, true);
            foreach (var item in garageItems) inventory.RestoreConsumed(item.Id);
            string[] memoryIds = { "garage_first_memory", "garage_second_memory", "garage_third_memory", "garage_final_memory" };
            for (int i = 0; i < memoryIds.Length; i++) memories.Collect(memoryIds[i], garageMemories[i]);
            if (checkpoint != TesterCheckpoint.SchoolCourtyardOpen && checkpoint != TesterCheckpoint.ParkExitOpen) return;
            variables.Set(schoolVariables, "533df6a400b34918b24179ce48127816", true);
            foreach (string id in new[] { "first_aid_kit_found", "ice_pack_found", "teacher_ruler_found", "tissue_box_found", "vending_coin_found",
                "school_supplies_returned", "school_ambulance_called" })
                variables.Set(schoolVariables, id, true);
            foreach (var item in schoolItems) inventory.RestoreConsumed(item.Id);
            string[] schoolIds = { "school_commentator_memory", "school_giles_second_memory", "school_coach_third_memory", "school_coach_fourth_memory", "school_call_ambulance" };
            for (int i = 0; i < schoolIds.Length; i++) memories.Collect(schoolIds[i], schoolMemories[i]);
            if (checkpoint != TesterCheckpoint.ParkExitOpen) return;
            foreach (string id in new[] { "a8f963194df740f4b6015ef9d0b71c22", "cinema_tickets_bought", "cinema_completed", "paired_pendant_bought", "mimes_completed" })
                variables.Set(parkVariables, id, true);
            variables.Set(parkBouquetVariables, "17ac1f87d5534776ac8743fb8e0aa992", true);
            variables.Set(parkMimesVariables, "mimes_purchase_unlocked", true);
            foreach (string id in new[] { "unlocked", "ordered", "coffee_read", "napkin_read", "completed" })
                variables.Set(parkCafeVariables, id, true);
            foreach (string id in new[] { "lily_spoken", "gazebo_reached", "exit_reached", "bouquet_returned", "bouquet_discarded" })
                variables.Set(parkDockVariables, id, true);
            foreach (var item in parkConsumedItems) inventory.RestoreConsumed(item.Id);
            inventory.TryAdd(parkReceipt);
            string[] parkIds = { "park-boat-conversation", "park-gazebo-conversation", "park-exit-conversation" };
            for (int i = 0; i < parkIds.Length; i++) memories.Collect(parkIds[i], parkMemories[i]);
        }

        public void ApplyScene(TesterCheckpoint checkpoint, Scene scene)
        {
            if (checkpoint == TesterCheckpoint.SchoolCourtyardOpen && scene.path == TestLocationLauncher.SchoolPath) RestoreSchoolCourtyard(scene);
            if (scene.path != TestLocationLauncher.HousePath) return;
            bool completed = checkpoint != TesterCheckpoint.HouseEntrance;
            // Восстанавливаем результат завершённой реакции, включая привязки с локализованными названиями.
            var house = LocationBindings.For<HouseBindings>(scene);
            house.TvStaticScreen.gameObject.SetActive(!completed);
            house.TvBaseballScreen.gameObject.SetActive(completed);
            house.RemoteInHand.gameObject.SetActive(completed);
        }

        private static void RestoreSchoolCourtyard(Scene scene)
        {
            // Восстанавливаем готовый этап без запуска игровых графов и воспоминаний.
            var bindings = LocationBindings.For<SchoolBindings>(scene);
            void State(ObjectStateTracker target, bool active, bool interactive)
            {
                target.GetComponent<ReactionInteractable>()?.SetInteractionEnabled(interactive);
                target.gameObject.SetActive(active);
            }
            State(bindings.SpectatorCommentator, true, false);
            State(bindings.Lily, false, false);
            State(bindings.Giles, true, false);
            State(bindings.Coach, false, false);
            State(bindings.FirstAidCabinet, true, false);
            State(bindings.IceContainer, true, false);
            State(bindings.TeacherRuler, false, false);
            State(bindings.TissueBox, false, false);
            State(bindings.VendingCoin, false, false);
            State(bindings.Payphone, true, false);
            State(bindings.Ambulance, true, true);
            State(bindings.AmbulanceBeacons, true, false);
            State(bindings.YardAnimation, true, false);
            State(bindings.AmbulanceDoctor, true, false);
            State(bindings.EnvironmentCorridor, true, false);
            State(bindings.EnvironmentLockerRooms, true, false);
            State(bindings.GymDoorClosed, false, false);
            State(bindings.GymDoorOpen, true, false);
            State(bindings.InteriorAnimation, true, false);
            State(bindings.CourtyardLeftClosed, false, false);
            State(bindings.CourtyardLeftOpen, true, false);
            State(bindings.CourtyardRightClosed, false, false);
            State(bindings.CourtyardRightOpen, true, false);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true)) tracker.Capture();
            Physics.SyncTransforms();
        }
    }
}
