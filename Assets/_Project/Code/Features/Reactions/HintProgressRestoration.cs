using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Compatibility with saves made before hint completion was stored in custom variables.
    // Called once when binding a location; live progress is written by reaction graphs.
    public static class HintProgressRestoration
    {
        public static void Restore(QuestInventory inventory, CustomVariableStore variables, MemoryCollection memories)
        {
            foreach (var bank in Resources.FindObjectsOfTypeAll<CustomVariableBank>())
            {
                RestoreValue(bank, variables, "remote_battery_first_found", inventory.WasCollected("quest.battery_aa"));
                RestoreValue(bank, variables, "remote_battery_second_found", inventory.WasCollected("quest.battery_aa_02"));
                RestoreValue(bank, variables, "father_compressor_delivered", inventory.IsConsumed("garage_compressor"));
                RestoreValue(bank, variables, "father_final_items_delivered", memories.Contains("garage_final_memory") ||
                    (inventory.IsConsumed("garage_vacuumhose") && inventory.IsConsumed("garage_siliconespray") && inventory.IsConsumed("garage_paperwipes")));
                RestoreValue(bank, variables, "school_supplies_returned", memories.Contains("school_coach_fourth_memory") ||
                    (inventory.IsConsumed("school_first_aid_kit") && inventory.IsConsumed("school_ice_pack") &&
                     inventory.IsConsumed("school_teacher_ruler") && inventory.IsConsumed("school_tissue_box")));
                RestoreValue(bank, variables, "school_ambulance_called", memories.Contains("school_call_ambulance") || inventory.IsConsumed("school_vending_coin"));
                RestoreValue(bank, variables, "a8f963194df740f4b6015ef9d0b71c22", inventory.WasCollected("park.flowers"));
            }
        }

        private static void RestoreValue(CustomVariableBank bank, CustomVariableStore variables, string id, bool completed)
        {
            if (completed && bank.Find(id) != null && !variables.HasSavedValue(bank, id)) variables.Set(bank, id, true);
        }
    }
}
