using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class PauseMenuView : MonoBehaviour, IPauseView
    {
        public event Action ResumeRequested;
        public event Action QuitRequested;
        private GameObject panel;
        private Button resume;

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
            panel = new GameObject("Pause panel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0, 0, 0, .75f);
            CreateText(panel.transform, "Пауза", 40, 110);
            resume = CreateButton("Продолжить", 10, () => ResumeRequested?.Invoke());
            CreateButton("Выйти из игры", -75, () => QuitRequested?.Invoke());
            panel.SetActive(false);
        }

        private static RectTransform Center(Transform parent, string name, float y, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0, y);
            return rect;
        }

        private static void CreateText(Transform parent, string caption, int size, float y)
        {
            var rect = Center(parent, caption, y, new Vector2(420, 65));
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = caption;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
        }

        private Button CreateButton(string caption, float y, Action action)
        {
            var rect = Center(panel.transform, caption, y, new Vector2(420, 65));
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(.22f, .25f, .3f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => action());
            CreateText(rect, caption, 26, 0);
            return button;
        }

        public void Show(bool visible)
        {
            panel.SetActive(visible);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? resume.gameObject : null);
        }
    }
}
