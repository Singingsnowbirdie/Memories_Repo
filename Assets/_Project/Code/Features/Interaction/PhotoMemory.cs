using System.Collections;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    public sealed class PhotoMemory : Interactable
    {
        private const string SaveKey = "Memories.Progress.CouplePhoto.v1";
        [SerializeField] private Renderer photoRenderer;
        [SerializeField] private int materialIndex = 1;
        [SerializeField] private Shader revealShader;
        [SerializeField] private GameObject rememberedWoman;
        [UnityEngine.Serialization.FormerlySerializedAs("firstInteraction")]
        [SerializeField] private ReactionGraph interactionGraph;
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private string revealedVariableId;
        private CustomVariableStore variables;
        private ReactionRunner runner;
        private StoryTextPresenter texts;
        private Material photoMaterial, blendMaterial;
        private Material original;
        private RenderTexture revealedTexture;
        private Texture photo;
        private bool revealed;
        public override string Prompt => "Рассмотреть фотографию";
        public bool IsRevealed => revealed;
        public override bool CanInteract => base.CanInteract && texts != null && !texts.IsPlaying &&
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
            if (PlayerPrefs.HasKey(SaveKey))
            {
                if (!this.variables.HasSavedValue(variableBank, revealedVariableId))
                    this.variables.Set(variableBank, revealedVariableId, PlayerPrefs.GetInt(SaveKey, 0) == 1);
                ResetProgress();
            }
            runner?.Dispose();
            runner = new ReactionRunner(presenter, memories, this, inventory: inventory, variables: this.variables);
        }

        private void Awake()
        {
            var materials = photoRenderer.sharedMaterials;
            original = materials[materialIndex];
            photoMaterial = new Material(original);
            photo = original.mainTexture;
            variables ??= new CustomVariableStore();
            // Migrate existing photo progress once; subsequent state lives in the variable bank.
            if (PlayerPrefs.HasKey(SaveKey))
            {
                if (!variables.HasSavedValue(variableBank, revealedVariableId))
                    variables.Set(variableBank, revealedVariableId, PlayerPrefs.GetInt(SaveKey, 0) == 1);
            }
            revealed = variables.Get(variableBank, revealedVariableId);
            UpdateWomanVisibility();
            materials[materialIndex] = photoMaterial;
            photoRenderer.sharedMaterials = materials;
            if (revealed) return;
            blendMaterial = new Material(revealShader);
            revealedTexture = new RenderTexture(photo.width, photo.height, 0);
            revealedTexture.Create();
            blendMaterial.SetFloat("_Reveal", 0);
            Graphics.Blit(photo, revealedTexture, blendMaterial);
            photoMaterial.mainTexture = revealedTexture;
            materials[materialIndex] = photoMaterial;
            photoRenderer.sharedMaterials = materials;
        }

        public override void Interact()
        {
            if (CanInteract) runner.TryRun(interactionGraph);
        }

        public void Remember()
        {
            if (revealed) return;
            revealed = true;
            variables.Set(variableBank, revealedVariableId, true);
            UpdateWomanVisibility();
            StartCoroutine(Reveal());
        }

        private void UpdateWomanVisibility()
        {
            if (rememberedWoman != null) rememberedWoman.SetActive(revealed);
        }

        private IEnumerator Reveal()
        {
            float elapsed = 0;
            while (elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                blendMaterial.SetFloat("_Reveal", Mathf.SmoothStep(0, 1, elapsed / 3f));
                Graphics.Blit(photo, revealedTexture, blendMaterial);
                yield return null;
            }
            photoMaterial.mainTexture = photo;
        }

        private void OnDisable() => runner?.Stop();

        private void OnDestroy()
        {
            runner?.Dispose();
            if (photoRenderer != null && original != null)
            {
                var materials = photoRenderer.sharedMaterials;
                materials[materialIndex] = original;
                photoRenderer.sharedMaterials = materials;
            }
            if (revealedTexture != null) { revealedTexture.Release(); Destroy(revealedTexture); }
            if (photoMaterial != null) Destroy(photoMaterial);
            if (blendMaterial != null) Destroy(blendMaterial);
        }
    }
}
