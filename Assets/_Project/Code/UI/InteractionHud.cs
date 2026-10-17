using Memories.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class InteractionHud : MonoBehaviour, IInteractionView
    {
        private Canvas canvas;
        private Text reticle;
        private Text hint;

        private void Awake()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            reticle = CreateText("Reticle", font, 28, Vector2.zero, new Vector2(60, 60));
            hint = CreateText("Hint", font, 22, new Vector2(0, -55), new Vector2(700, 60));
            Show(false, false, "");
        }

        private Text CreateText(string label, Font font, int size, Vector2 position, Vector2 dimensions)
        {
            var item = new GameObject(label, typeof(RectTransform));
            item.transform.SetParent(transform, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = item.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var shadow = item.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -1);
            return text;
        }

        public void Show(bool visible, bool available, string prompt)
        {
            canvas.enabled = visible;
            reticle.text = available ? "+" : "•";
            reticle.color = available ? new Color(1, .85f, .35f) : Color.white;
            hint.text = prompt;
        }
    }
}
