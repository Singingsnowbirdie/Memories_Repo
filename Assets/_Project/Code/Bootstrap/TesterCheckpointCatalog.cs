using System;
using System.Linq;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    public enum TesterCheckpoint { HouseEntrance, HouseMemoryComplete, BackyardMemoryComplete, GarageCorridorOpen, SchoolCourtyardOpen }

    public sealed class TesterCheckpointCatalog : ScriptableObject
    {
        public CustomVariableBank houseVariables, backyardVariables;
        public MemoryGraph houseMemory, backyardFirstMemory, backyardSecondMemory;
        public QuestItem[] houseItems, backyardItems;
        public Vector3 houseMemoryPosition;
        public float houseMemoryYaw;
        public CustomVariableBank garageVariables;
        public MemoryGraph[] garageMemories;
        public QuestItem[] garageItems;
        public CustomVariableBank schoolVariables;
        public MemoryGraph[] schoolMemories;
        public QuestItem[] schoolItems;

        public void Seed(TesterCheckpoint checkpoint, CustomVariableStore variables, QuestInventory inventory, MemoryCollection memories)
        {
            if (checkpoint == TesterCheckpoint.HouseEntrance) return;
            variables.Set(houseVariables, "a658183a452d4c2b9ef92252e09264ec", true);
            variables.Set(houseVariables, "woman_missing_remote_read", true);
            variables.Set(houseVariables, "television_on", true);
            foreach (var item in houseItems) inventory.RestoreConsumed(item.Id);
            memories.Collect("house-tv-baseball", houseMemory);
            if (checkpoint != TesterCheckpoint.BackyardMemoryComplete && checkpoint != TesterCheckpoint.GarageCorridorOpen && checkpoint != TesterCheckpoint.SchoolCourtyardOpen) return;
            foreach (string id in new [] {"ben_alex_inscription_read", "ben_home_first_interaction", "optional_interactives_memory_started",
                "fishing_rods_inspected", "hose_inspected", "bicycle_inspected", "waterguns_inspected", "briefcase_inspected", "wheelbarrow_inspected",
                "baseball_equipment_revealed", "ben_received_baseball", "ben_received_cap", "baseball_bat_found"})
                variables.Set(backyardVariables, id, true);
            variables.Set(backyardVariables, "optional_interactives_inspected", 6);
            foreach (var item in backyardItems) inventory.RestoreConsumed(item.Id);
            memories.Collect("backyard_optional_memory", backyardFirstMemory);
            memories.Collect(BackyardGarageAttachment.MemoryId, backyardSecondMemory);
            if (checkpoint != TesterCheckpoint.GarageCorridorOpen && checkpoint != TesterCheckpoint.SchoolCourtyardOpen) return;
            foreach (string id in new[] { "father_compressor_requested", "father_hood_items_requested", "father_hood_items_delivered" })
                variables.Set(garageVariables, id, true);
            foreach (var item in garageItems) inventory.RestoreConsumed(item.Id);
            string[] memoryIds = { "garage_first_memory", "garage_second_memory", "garage_third_memory", "garage_final_memory" };
            for (int i = 0; i < memoryIds.Length; i++) memories.Collect(memoryIds[i], garageMemories[i]);
            if (checkpoint != TesterCheckpoint.SchoolCourtyardOpen) return;
            variables.Set(schoolVariables, "533df6a400b34918b24179ce48127816", true);
            foreach (var item in schoolItems) inventory.RestoreConsumed(item.Id);
            string[] schoolIds = { "school_commentator_memory", "school_giles_second_memory", "school_coach_third_memory", "school_coach_fourth_memory", "school_call_ambulance" };
            for (int i = 0; i < schoolIds.Length; i++) memories.Collect(schoolIds[i], schoolMemories[i]);
        }

        public void ApplyScene(TesterCheckpoint checkpoint, Scene scene)
        {
            if (checkpoint == TesterCheckpoint.SchoolCourtyardOpen && scene.path == TestLocationLauncher.SchoolPath) RestoreSchoolCourtyard(scene);
            if (scene.path != TestLocationLauncher.HousePath) return;
            bool completed = checkpoint != TesterCheckpoint.HouseEntrance;
            // Восстанавливаем результат завершённой реакции, включая привязки с локализованными названиями.
            Set(scene, "Гостиная/Prop_OldTV/TV Static Screen", !completed);
            Set(scene, "Гостиная/Prop_OldTV/TV Baseball Screen", completed);
            Set(scene, "Гостиная/FEMALE Gray Seated/Remote in hand", completed);
        }

        private static void RestoreSchoolCourtyard(Scene scene)
        {
            // Восстанавливаем готовый этап без запуска игровых графов и воспоминаний.
            var trackers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ObjectStateTracker>(true))
                .ToDictionary(tracker => tracker.PersistentId);
            void State(string id, bool active, bool interactive)
            {
                var target = trackers[id];
                target.GetComponent<ReactionInteractable>()?.SetInteractionEnabled(interactive);
                target.gameObject.SetActive(active);
            }
            State("school.spectator_commentator", true, false);
            State("school.lily", false, false);
            State("school.giles", true, false);
            State("school.coach", false, false);
            State("school.first_aid_cabinet", true, false);
            State("school.ice_container", true, false);
            State("school.teacher_ruler", false, false);
            State("school.tissue_box", false, false);
            State("school.vending_coin", false, false);
            State("school.payphone", true, false);
            State("school.ambulance", true, true);
            State("school.ambulance_doctor", true, false);
            State("school.classroom_102_door", true, false);
            var door = trackers["school.classroom_102_door"];
            door.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Петля").localRotation *= Quaternion.Euler(0, 100, 0);
            var entrance = trackers["school.gym_entrance"].GetComponent<SchoolGymEntranceReaction>();
            entrance.RestoreState("corridor-open");
            entrance.RestoreForDirectLoad();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var progress in root.GetComponentsInChildren<SchoolFirstAidProgressReaction>(true))
                    progress.RestoreState(@"{""started"":true,""returned"":true,""hints"":""""}");
            trackers["school.payphone"].GetComponent<SchoolAmbulanceArrivalReaction>().RestoreState("open");
            foreach (var tracker in trackers.Values) tracker.Capture();
            Physics.SyncTransforms();
        }
        public static Transform Find(Scene scene, string path)
        {
            int split = path.IndexOf('/');
            string rootName = split < 0 ? path : path.Substring(0, split);
            var root = scene.GetRootGameObjects().SingleOrDefault(g => g.name == rootName);
            var target = root == null ? null : split < 0 ? root.transform : root.transform.Find(path.Substring(split + 1));
            return target != null ? target : throw new InvalidOperationException("Checkpoint object missing: " + path);
        }
        private static void Set(Scene scene, string path, bool active) => Find(scene, path).gameObject.SetActive(active);
    }
}
