using Memories.Gameplay.Interaction;
using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графами после передачи Бену мяча или кепки, подбора биты и её расходования после игры.
    /// Разрешает взаимодействие с площадкой и включает её подсветку, только когда у Бена есть мяч и кепка,
    /// у игрока есть бита, а второе воспоминание ещё не просмотрено. Иначе отключает взаимодействие и подсветку.
    /// При Initialize однократно восстанавливает окно и расход биты по сохранённому воспоминанию, затем обновляет площадку.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionInteractable))]
    public sealed class RefreshBaseballAreaReaction : CustomReaction
    {
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private QuestItem bat;
        [SerializeField] private Renderer surface;
        [SerializeField] private Material readyMaterial;
        [SerializeField] private string memoryId;
        [SerializeField] private GameObject intactWindow;
        [SerializeField] private GameObject brokenWindow;
        private QuestInventory inventory;
        private CustomVariableStore variables;
        private MemoryCollection memories;
        private Material originalMaterial;
        private ReactionInteractable interactable;
        public bool IsReady { get; private set; }

        public void Initialize(QuestInventory inventory, CustomVariableStore variables, MemoryCollection memories)
        {
            this.inventory = inventory;
            this.variables = variables;
            this.memories = memories;
            interactable = GetComponent<ReactionInteractable>();
            if (surface != null && originalMaterial == null) originalMaterial = surface.sharedMaterial;
            // Restore a loaded location once. Live consequences belong to explicit graph nodes.
            bool completed = memories.Contains(memoryId);
            if (completed && bat != null && inventory.Contains(bat.Id)) inventory.TryConsume(bat);
            if (intactWindow != null) intactWindow.SetActive(!completed);
            if (brokenWindow != null) brokenWindow.SetActive(completed);
            Execute();
        }

        public override void Execute()
        {
            if (interactable == null || inventory == null || variables == null || memories == null) return;
            IsReady = variableBank != null && bat != null &&
                variables.Get(variableBank, "ben_received_cap") &&
                variables.Get(variableBank, "ben_received_baseball") &&
                inventory.Contains(bat.Id) && !memories.Contains(memoryId);
            interactable.SetInteractionEnabled(IsReady);
            if (surface != null && readyMaterial != null)
                surface.sharedMaterial = IsReady ? readyMaterial : originalMaterial;
        }
    }
}
