using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class PhotoMemory : Interactable
    {
        private const string SaveKey = "Memories.Progress.CouplePhoto.v1";
        [SerializeField] private GameObject rememberedWoman;
        [UnityEngine.Serialization.FormerlySerializedAs("firstInteraction")]
        [SerializeField] private ReactionGraph interactionGraph;
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private string revealedVariableId;
        private CustomVariableStore variables;
        [SerializeField, InspectorName("Объекты графа")] private ReactionObjectBinding[] objectBindings = new ReactionObjectBinding[0];
        private ReactionRunner runner;
        private StoryTextPresenter texts;
        private bool revealed;
        public override string Prompt => "Рассмотреть фотографию";
        public bool IsRevealed => revealed;
        public override bool CanInteract => base.CanInteract && texts != null && !texts.BlocksInteractions &&
            runner != null && runner.CanRun(interactionGraph);

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        public void Initialize(StoryTextPresenter presenter, MemoryPresenter memories, QuestInventory inventory = null, CustomVariableStore variables = null)
        {
            texts = presenter;
            this.variables = variables ?? new CustomVariableStore();
            if (this.variables.PersistProgress && PlayerPrefs.HasKey(SaveKey))
            {
                if (!this.variables.HasSavedValue(variableBank, revealedVariableId))
                    this.variables.Set(variableBank, revealedVariableId, PlayerPrefs.GetInt(SaveKey, 0) == 1);
                ResetProgress();
            }
            runner?.Dispose();
            runner = new ReactionRunner(presenter, memories, this, inventory: inventory, variables: this.variables, objectBindings: objectBindings);
            revealed = this.variables.Get(variableBank, revealedVariableId);
            UpdateWomanVisibility();
            GetComponent<RevealPhotoReaction>()?.Initialize(revealed);
        }

        public override void Interact()
        {
            if (CanInteract) runner.TryRun(interactionGraph);
        }

        private void UpdateWomanVisibility()
        {
            if (rememberedWoman != null) rememberedWoman.SetActive(revealed);
        }

        private void OnDisable() => runner?.Stop();
        private void OnDestroy() => runner?.Dispose();
    }
}
