using System.Collections.Generic;
using Memories.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class InventoryHud : MonoBehaviour
    {
        private QuestInventory inventory;
        private GameplayModeState mode;
        private Canvas canvas;
        private RectTransform panel;
        private Font font;
        private const float SlideDuration = .8f;
        private float slideProgress;
        private readonly List<GameObject> rows = new List<GameObject>();

        public void Initialize(QuestInventory inventory, GameplayModeState mode)
        {
            this.inventory = inventory;
            this.mode = mode;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = Rect(transform, "Inventory panel", new Vector2(260, 90));
            panel.anchorMin = panel.anchorMax = new Vector2(0, .5f);
            panel.pivot = new Vector2(0, .5f);
            panel.anchoredPosition = new Vector2(-panel.sizeDelta.x, 0);
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0, 0, 0, .5f);
            background.raycastTarget = false;
            inventory.Changed += Refresh;
            Refresh();
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            canvas.enabled = inventory.Items.Count > 0 && mode.Mode == GameplayMode.Playing;
            if (!canvas.enabled) return;
            slideProgress = Mathf.Min(1, slideProgress + Time.deltaTime / SlideDuration);
            panel.anchoredPosition = new Vector2(
                Mathf.Lerp(-panel.sizeDelta.x, 24, Mathf.SmoothStep(0, 1, slideProgress)), 0);
        }

        private void Refresh()
        {
            foreach (var row in rows) { row.SetActive(false); Destroy(row); }
            rows.Clear();
            canvas.enabled = inventory.Items.Count > 0 && mode.Mode == GameplayMode.Playing;
            if (inventory.Items.Count == 0)
            {
                slideProgress = 0;
                panel.anchoredPosition = new Vector2(-panel.sizeDelta.x, 0);
                return;
            }
            float rowHeight = Mathf.Min(56, 560f / Mathf.Max(1, inventory.Items.Count));
            panel.sizeDelta = new Vector2(260, 44 + Mathf.Max(1, inventory.Items.Count) * rowHeight);
            Label(panel, "Инвентарь", new Vector2(12, -8), new Vector2(236, 30), 22);
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                float y = -44 - i * rowHeight;
                float iconSize = Mathf.Min(40, rowHeight - 4);
                var icon = Rect(panel, "Item icon", new Vector2(iconSize, iconSize));
                icon.anchoredPosition = new Vector2(12, y - 2);
                var image = icon.gameObject.AddComponent<Image>();
                image.sprite = item.Icon;
                image.preserveAspect = true;
                image.color = item.Icon != null ? Color.white : new Color(1, 1, 1, .12f);
                image.raycastTarget = false;
                rows.Add(icon.gameObject);
                Label(panel, item.DisplayName, new Vector2(60, y), new Vector2(188, rowHeight), 18);
            }
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size;
            return rect;
        }

        private void Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Rect(parent, "Inventory label", size);
            rect.anchoredPosition = position;
            var text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.raycastTarget = false;
            rows.Add(rect.gameObject);
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }
    }
}
