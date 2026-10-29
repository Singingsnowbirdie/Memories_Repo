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
            panel = Rect(transform, "Inventory panel", new Vector2(120, 136));
            panel.anchorMin = panel.anchorMax = new Vector2(0, .5f);
            panel.pivot = new Vector2(0, .5f);
            panel.anchoredPosition = new Vector2(-panel.sizeDelta.x, 0);
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
            float rowHeight = Mathf.Min(136, 816f / Mathf.Max(1, inventory.Items.Count));
            panel.sizeDelta = new Vector2(120, inventory.Items.Count * rowHeight);
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                var item = inventory.Items[i];
                float y = -i * rowHeight;
                float iconSize = Mathf.Min(120, rowHeight - 16);
                var icon = Rect(panel, "Item icon", new Vector2(iconSize, iconSize));
                icon.anchoredPosition = new Vector2(0, y - (rowHeight - iconSize) * .5f);
                var image = icon.gameObject.AddComponent<Image>();
                image.sprite = item.Icon;
                image.preserveAspect = true;
                image.color = item.Icon != null ? Color.white : Color.clear;
                image.raycastTarget = false;
                rows.Add(icon.gameObject);
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

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }
    }
}
