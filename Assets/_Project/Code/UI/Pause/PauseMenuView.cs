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
        public event Action SettingsRequested;
        public event Action BackRequested;
        public event Action<float> MasterChanged;
        public event Action<float> MusicChanged;
        public event Action<float> SoundsChanged;
        private GameObject panel;
        private GameObject menuContent;
        private GameObject settingsContent;
        private Slider masterSlider, musicSlider, soundsSlider;
        private Button settingsButton;
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
            menuContent = new GameObject("Pause actions", typeof(RectTransform));
            menuContent.transform.SetParent(panel.transform, false);
            settingsContent = new GameObject("Audio settings", typeof(RectTransform));
            settingsContent.transform.SetParent(panel.transform, false);
            CreateText(menuContent.transform, "Пауза", 40, 110);
            resume = CreateButton(menuContent.transform, "Продолжить", 10, () => ResumeRequested?.Invoke());
            settingsButton = CreateButton(menuContent.transform, "Настройки", -75, () => SettingsRequested?.Invoke());
            CreateButton(menuContent.transform, "Выйти из игры", -160, () => QuitRequested?.Invoke());
            CreateText(settingsContent.transform, "Настройки звука", 40, 240);
            masterSlider = CreateSlider("Общая громкость", 120, value => MasterChanged?.Invoke(value));
            musicSlider = CreateSlider("Музыка", 0, value => MusicChanged?.Invoke(value));
            soundsSlider = CreateSlider("Другие звуки", -120, value => SoundsChanged?.Invoke(value));
            CreateButton(settingsContent.transform, "Назад", -240, () => BackRequested?.Invoke());
            settingsContent.SetActive(false);
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

        private Button CreateButton(Transform parent, string caption, float y, Action action)
        {
            var rect = Center(parent, caption, y, new Vector2(420, 65));
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(.22f, .25f, .3f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => action());
            CreateText(rect, caption, 26, 0);
            return button;
        }

        private Slider CreateSlider(string caption, float y, Action<float> changed)
        {
            var label = CreateLabel(settingsContent.transform, caption, y + 40);
            var rect = Center(settingsContent.transform, caption + " slider", y, new Vector2(420, 32));
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(.22f, .25f, .3f);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            var area = Center(rect, "Handle area", 0, new Vector2(400, 32));
            var handle = Center(area, "Handle", 0, new Vector2(20, 40));
            var graphic = handle.gameObject.AddComponent<Image>();
            slider.handleRect = handle;
            slider.targetGraphic = graphic;
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(value =>
            {
                label.text = caption + "  " + Mathf.RoundToInt(value * 100) + "%";
                changed(value);
            });
            return slider;
        }

        private static Text CreateLabel(Transform parent, string caption, float y)
        {
            CreateText(parent, caption, 26, y);
            return parent.GetChild(parent.childCount - 1).GetComponent<Text>();
        }

        private static void SetSlider(Slider slider, float value, string caption)
        {
            slider.SetValueWithoutNotify(value);
            // Each slider immediately follows its label in the settings hierarchy.
            slider.transform.parent.GetChild(slider.transform.GetSiblingIndex() - 1)
                .GetComponent<Text>().text = caption + "  " + Mathf.RoundToInt(value * 100) + "%";
        }

        public void ShowSettings(bool visible, float master, float music, float sounds)
        {
            menuContent.SetActive(!visible);
            settingsContent.SetActive(visible);
            if (visible)
            {
                SetSlider(masterSlider, master, "Общая громкость");
                SetSlider(musicSlider, music, "Музыка");
                SetSlider(soundsSlider, sounds, "Другие звуки");
            }
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? masterSlider.gameObject : settingsButton.gameObject);
        }

        public void Show(bool visible)
        {
            menuContent.SetActive(true);
            settingsContent.SetActive(false);
            panel.SetActive(visible);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? resume.gameObject : null);
        }
    }
}
