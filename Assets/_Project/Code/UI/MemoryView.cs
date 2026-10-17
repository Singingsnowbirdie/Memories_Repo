using System;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class MemoryView : MonoBehaviour, IMemoryView
    {
        public event Action CloseRequested;
        private GameObject panel;
        private Text heading;
        private Text body;
        private Button close;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            panel = new GameObject("Memory panel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0, 0, 0, .85f);
            heading = MakeText("Title", 40, 150, 90);
            body = MakeText("Text", 28, 0, 180);
            var buttonRect = Center("Return", -190, new Vector2(420, 65));
            var background = buttonRect.gameObject.AddComponent<Image>();
            background.color = new Color(.22f, .25f, .3f);
            close = buttonRect.gameObject.AddComponent<Button>();
            close.targetGraphic = background;
            close.onClick.AddListener(() => CloseRequested?.Invoke());
            var label = MakeText("Return label", 26, -190, 65);
            label.text = "Вернуться";
            Hide();
        }

        private RectTransform Center(string name, float y, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(panel.transform, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0, y);
            return rect;
        }

        private Text MakeText(string name, int size, float y, float height)
        {
            var text = Center(name, y, new Vector2(900, height)).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        public void Show(string title, string text)
        {
            heading.text = title;
            body.text = text;
            panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
        }

        public void Hide()
        {
            panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
