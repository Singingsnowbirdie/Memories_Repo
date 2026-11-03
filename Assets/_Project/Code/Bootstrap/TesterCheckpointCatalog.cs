using System;
using System.Linq;
using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Bootstrap
{
    public enum TesterCheckpoint { HouseEntrance, HouseMemoryComplete, BackyardMemoryComplete }

    public sealed class TesterCheckpointCatalog : ScriptableObject
    {
        public CustomVariableBank houseVariables, backyardVariables;
        public MemoryGraph houseMemory, backyardFirstMemory, backyardSecondMemory;
        public QuestItem[] houseItems, backyardItems;
        public Vector3 houseMemoryPosition;
        public float houseMemoryYaw;

        public void Seed(TesterCheckpoint checkpoint, CustomVariableStore variables, QuestInventory inventory, MemoryCollection memories)
        {
            if (checkpoint == TesterCheckpoint.HouseEntrance) return;
            variables.Set(houseVariables, "a658183a452d4c2b9ef92252e09264ec", true);
            variables.Set(houseVariables, "woman_missing_remote_read", true);
            variables.Set(houseVariables, "television_on", true);
            foreach (var item in houseItems) inventory.RestoreConsumed(item.Id);
            memories.Collect("house-tv-baseball", houseMemory);
            if (checkpoint != TesterCheckpoint.BackyardMemoryComplete) return;
            foreach (string id in new [] {"ben_alex_inscription_read", "ben_home_first_interaction", "optional_interactives_memory_started",
                "fishing_rods_inspected", "hose_inspected", "bicycle_inspected", "waterguns_inspected", "briefcase_inspected", "wheelbarrow_inspected",
                "baseball_equipment_revealed", "ben_received_baseball", "ben_received_cap", "baseball_bat_found"})
                variables.Set(backyardVariables, id, true);
            variables.Set(backyardVariables, "optional_interactives_inspected", 6);
            foreach (var item in backyardItems) inventory.RestoreConsumed(item.Id);
            memories.Collect("backyard_optional_memory", backyardFirstMemory);
            memories.Collect(BackyardGarageAttachment.MemoryId, backyardSecondMemory);
        }

        public void ApplyScene(TesterCheckpoint checkpoint, Scene scene)
        {
            if (scene.path != TestLocationLauncher.HousePath) return;
            bool completed = checkpoint != TesterCheckpoint.HouseEntrance;
            // Match the completed reaction presentation, including bindings whose display names are localized.
            Set(scene, "Гостиная/Prop_OldTV/TV Static Screen", !completed);
            Set(scene, "Гостиная/Prop_OldTV/TV Baseball Screen", completed);
            Set(scene, "Гостиная/FEMALE Gray Seated/Remote in hand", completed);
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
