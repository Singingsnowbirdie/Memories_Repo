using System.Collections;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается узлом «Появление фотографии» графа Photo_Interaction при взаимодействии с рамкой,
    /// когда граф выбирает ветку проявления фотографии.
    /// Плавно проявляет снимок, приближает к нему камеру и возвращает её обратно; на время показа блокирует управление.
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
        private UI.GameplayModeState gameplayState;
        private Player.PlayerMotor player;
        private Transform focusedCamera;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private bool previousCameraControl;

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        public void Restore(CustomVariableStore variables, UI.GameplayModeState gameplayState = null,
            Player.PlayerMotor player = null)
        {
            this.gameplayState = gameplayState;
            this.player = player;
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

        private IEnumerator Reveal()
        {
            gameplayState?.BlockControls(this);
            try
            {
                IEnumerator cameraMovement = null;
                if (player != null && player.CameraTransform != null)
                {
                    focusedCamera = player.CameraTransform;
                    originalCameraPosition = focusedCamera.localPosition;
                    originalCameraRotation = focusedCamera.localRotation;
                    previousCameraControl = player.CameraPoseControlled;
                    player.CameraPoseControlled = true;
                    var center = photoRenderer.bounds.center;
                    var offset = focusedCamera.position - center;
                    float distance = Mathf.Min(offset.magnitude, Mathf.Max(.75f, photoRenderer.bounds.size.magnitude));
                    var targetPosition = center + offset.normalized * distance;
                    var targetRotation = Quaternion.LookRotation(center - targetPosition, Vector3.up);
                    cameraMovement = MoveCamera(focusedCamera.parent.InverseTransformPoint(targetPosition),
                        Quaternion.Inverse(focusedCamera.parent.rotation) * targetRotation);
                }
                float elapsed = 0;
                while (elapsed < 3f)
                {
                    if (cameraMovement != null && !cameraMovement.MoveNext()) cameraMovement = null;
                    elapsed += Time.deltaTime;
                    blendMaterial.SetFloat("_Reveal", Mathf.SmoothStep(0, 1, elapsed / 3f));
                    Graphics.Blit(photo, revealedTexture, blendMaterial);
                    yield return null;
                }
                photoMaterial.mainTexture = photo;
                if (focusedCamera != null) yield return MoveCamera(originalCameraPosition, originalCameraRotation);
            }
            finally { RestoreCamera(); gameplayState?.ReleaseControls(this); }
        }

        private IEnumerator MoveCamera(Vector3 position, Quaternion rotation)
        {
            var fromPosition = focusedCamera.localPosition;
            var fromRotation = focusedCamera.localRotation;
            float elapsed = 0;
            while (elapsed < .8f && focusedCamera != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / .8f);
                focusedCamera.SetLocalPositionAndRotation(Vector3.Lerp(fromPosition, position, t),
                    Quaternion.Slerp(fromRotation, rotation, t));
                yield return null;
            }
        }

        private void RestoreCamera()
        {
            if (focusedCamera == null) return;
            focusedCamera.SetLocalPositionAndRotation(originalCameraPosition, originalCameraRotation);
            focusedCamera = null;
            if (player != null) player.CameraPoseControlled = previousCameraControl;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            RestoreCamera();
            gameplayState?.ReleaseControls(this);
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
