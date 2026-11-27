using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается узлом «Появление фотографии» графа Photo_Interaction при взаимодействии с рамкой,
    /// когда граф выбирает ветку проявления фотографии.
    /// Плавно проявляет снимок, не изменяя камеру и не блокируя управление.
    /// Restore отдельно восстанавливает уже проявленное фото по сохранённой переменной без повторной анимации.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Memories/Reactions/Reveal Photo Reaction")]
    public sealed class RevealPhotoReaction : CustomReaction
    {
        private const string SaveKey = "Memories.Progress.CouplePhoto.v1";
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private string revealedVariableId;
        [SerializeField] private Renderer photoRenderer;
        [SerializeField] private int materialIndex = 1;
        [SerializeField] private Shader revealShader;
        private Material photoMaterial, blendMaterial, original;
        private RenderTexture revealedTexture;
        private Texture photo;
        private bool initialized, revealed;
        private CancellationTokenSource revealCancellation;

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        public void Restore(CustomVariableStore variables)
        {
            if (variables.PersistProgress && PlayerPrefs.HasKey(SaveKey))
            {
                if (!variables.HasSavedValue(variableBank, revealedVariableId))
                    variables.Set(variableBank, revealedVariableId, PlayerPrefs.GetInt(SaveKey, 0) == 1);
                ResetProgress();
            }
            Initialize(variables.Get(variableBank, revealedVariableId));
        }

        public void Initialize(bool alreadyRevealed)
        {
            if (initialized) return;
            var materials = photoRenderer.sharedMaterials;
            original = materials[materialIndex];
            photoMaterial = new Material(original);
            photo = original.mainTexture;
            revealed = alreadyRevealed;
            materials[materialIndex] = photoMaterial;
            photoRenderer.sharedMaterials = materials;
            initialized = true;
            if (revealed) return;
            blendMaterial = new Material(revealShader);
            revealedTexture = new RenderTexture(photo.width, photo.height, 0);
            revealedTexture.Create();
            blendMaterial.SetFloat("_Reveal", 0);
            Graphics.Blit(photo, revealedTexture, blendMaterial);
            photoMaterial.mainTexture = revealedTexture;
        }

        public override void Execute()
        {
            Initialize(false);
            if (revealed) return;
            revealed = true;
            StartCoroutine(Reveal());
        }

        public override async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Initialize(false);
            if (revealed) return;
            revealed = true;
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, this.GetCancellationTokenOnDestroy());
            revealCancellation = lifetime;
            try { await Reveal().ToUniTask(cancellationToken: lifetime.Token); }
            finally { revealCancellation = null; }
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

        private void OnDisable()
        {
            revealCancellation?.Cancel();
            StopAllCoroutines();
            if (revealed && photoMaterial != null) photoMaterial.mainTexture = photo;
        }

        private void OnDestroy()
        {
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
