using System;
using System.Collections.Generic;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class PauseMenuView : MonoBehaviour, IPauseView
    {
        public event Action ResumeRequested;
        public event Action ReturnToStartRequested;
        public event Action RestartRequested;
        public event Action RestartConfirmed;
        public event Action RestartCancelled;
        public event Action QuitRequested;
        public event Action SettingsRequested;
        public event Action CreditsRequested;
        public event Action MemoriesRequested;
        public event Action<string> MemoryRequested;
        public event Action BackRequested;
        public event Action<float> MasterChanged;
        public event Action<float> MusicChanged;
        public event Action<float> SoundsChanged;
        private GameObject panel;
        private GameObject menuContent;
        private GameObject settingsContent;
        private GameObject creditsContent;
        private Button creditsButton;
        private Button creditsBack;
        private GameObject restartConfirmation;
        private Button cancelRestart;
        private Slider masterSlider, musicSlider, soundsSlider;
        private Button settingsButton;
        private Button resume;
        private Text hint;
        private int nextHint;
        private GameObject memoriesContent;
        private GameObject memoryCards;
        private Button memoriesButton, memoriesBack, previousPage, nextPage;
        private Text memoryPageLabel, emptyMemories;
        private IReadOnlyList<MemoryCollection.FoundMemory> foundMemories;
        private int memoryPage;
        private const int MemoriesPerPage = 4;
        private static readonly string[] Hints =
        {
            "Подсказка: удерживайте Shift, чтобы бежать",
            "Подсказка: если вы застряли, нажмите «Вернуться в начало локации» в меню паузы"
        };

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
            restartConfirmation = new GameObject("Restart confirmation", typeof(RectTransform));
            restartConfirmation.transform.SetParent(panel.transform, false);
            CreateText(restartConfirmation.transform, "Начать сначала?", 40, 150);
            CreateText(restartConfirmation.transform, "Прогресс будет сброшен", 26, 75);
            CreateButton(restartConfirmation.transform, "Начать сначала", -10, () => RestartConfirmed?.Invoke());
            cancelRestart = CreateButton(restartConfirmation.transform, "Отмена", -95, () => RestartCancelled?.Invoke());
            restartConfirmation.SetActive(false);
            CreateText(menuContent.transform, "Пауза", 40, 300);
            resume = CreateButton(menuContent.transform, "Продолжить", 200, () => ResumeRequested?.Invoke());
            memoriesButton = CreateButton(menuContent.transform, "Копилка воспоминаний", 115, () => MemoriesRequested?.Invoke());
            settingsButton = CreateButton(menuContent.transform, "Настройки", 30, () => SettingsRequested?.Invoke());
            CreateButton(menuContent.transform, "Вернуться в начало локации", -55, () => ReturnToStartRequested?.Invoke());
            CreateButton(menuContent.transform, "Начать сначала", -140, () => RestartRequested?.Invoke());
            creditsButton = CreateButton(menuContent.transform, "Титры", -225, () => CreditsRequested?.Invoke());
            CreateButton(menuContent.transform, "Выйти из игры", -310, () => QuitRequested?.Invoke());
            CreateMemoryLibrary();
            creditsContent = new GameObject("Credits", typeof(RectTransform));
            creditsContent.transform.SetParent(panel.transform, false);
            CreateText(creditsContent.transform, "Титры", 40, 195);
            creditsBack = CreateButton(creditsContent.transform, "Назад", -240, () => BackRequested?.Invoke());
            creditsContent.SetActive(false);
            CreateText(settingsContent.transform, "Настройки звука", 40, 240);
            masterSlider = CreateSlider("Общая громкость", 120, value => MasterChanged?.Invoke(value));
            musicSlider = CreateSlider("Музыка", 0, value => MusicChanged?.Invoke(value));
            soundsSlider = CreateSlider("Другие звуки", -120, value => SoundsChanged?.Invoke(value));
            CreateButton(settingsContent.transform, "Назад", -240, () => BackRequested?.Invoke());
            hint = CreateLabel(panel.transform, Hints[0], 0);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(.1f, 0);
            hintRect.anchorMax = new Vector2(.9f, 0);
            hintRect.pivot = new Vector2(.5f, 0);
            hintRect.sizeDelta = new Vector2(0, 65);
            hintRect.anchoredPosition = new Vector2(0, 24);
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

        public void ShowCredits(bool visible)
        {
            menuContent.SetActive(!visible);
            settingsContent.SetActive(false);
            restartConfirmation.SetActive(false);
            creditsContent.SetActive(visible);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? creditsBack.gameObject : creditsButton.gameObject);
        }

        public void ShowRestartConfirmation(bool visible)
        {
            menuContent.SetActive(!visible);
            settingsContent.SetActive(false);
            restartConfirmation.SetActive(visible);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? cancelRestart.gameObject : resume.gameObject);
        }

        public void Show(bool visible)
        {
            if (visible && !panel.activeSelf)
            {
                hint.text = Hints[nextHint];
                nextHint = (nextHint + 1) % Hints.Length;
            }
            menuContent.SetActive(true);
            settingsContent.SetActive(false);
            restartConfirmation.SetActive(false);
            creditsContent.SetActive(false);
            memoriesContent.SetActive(false);
            panel.SetActive(visible);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(visible ? resume.gameObject : null);
        }

        private void CreateMemoryLibrary()
        {
            memoriesContent = new GameObject("Memory collection", typeof(RectTransform));
            memoriesContent.transform.SetParent(panel.transform, false);
            var title = CreateLabel(memoriesContent.transform, "Копилка воспоминаний", 330);
            title.fontSize = 40;
            title.rectTransform.sizeDelta = new Vector2(800, 65);
            emptyMemories = CreateLabel(memoriesContent.transform, "Пока нет найденных воспоминаний", 60);
            emptyMemories.rectTransform.sizeDelta = new Vector2(800, 65);
            memoryCards = new GameObject("Found memories", typeof(RectTransform));
            memoryCards.transform.SetParent(memoriesContent.transform, false);
            previousPage = CreateButton(memoriesContent.transform, "←", -300, () => { memoryPage--; RefreshMemories(); });
            nextPage = CreateButton(memoriesContent.transform, "→", -300, () => { memoryPage++; RefreshMemories(); });
            foreach (var button in new[] { previousPage, nextPage })
            {
                ((RectTransform)button.transform).sizeDelta = new Vector2(100, 55);
                ((RectTransform)button.transform.GetChild(0)).sizeDelta = new Vector2(100, 55);
            }
            ((RectTransform)previousPage.transform).anchoredPosition = new Vector2(-200, -300);
            ((RectTransform)nextPage.transform).anchoredPosition = new Vector2(200, -300);
            memoryPageLabel = CreateLabel(memoriesContent.transform, "", -300);
            memoryPageLabel.rectTransform.sizeDelta = new Vector2(180, 55);
            memoriesBack = CreateButton(memoriesContent.transform, "Назад", -380, () => BackRequested?.Invoke());
            memoriesContent.SetActive(false);
        }

        public void ShowMemories(bool visible, IReadOnlyList<MemoryCollection.FoundMemory> memories)
        {
            menuContent.SetActive(!visible);
            settingsContent.SetActive(false);
            creditsContent.SetActive(false);
            restartConfirmation.SetActive(false);
            memoriesContent.SetActive(visible);
            foundMemories = memories;
            if (visible) RefreshMemories();
            else if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(memoriesButton.gameObject);
        }

        private void RefreshMemories()
        {
            foreach (Transform child in memoryCards.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            int count = foundMemories?.Count ?? 0;
            int pages = Mathf.Max(1, (count + MemoriesPerPage - 1) / MemoriesPerPage);
            memoryPage = Mathf.Clamp(memoryPage, 0, pages - 1);
            emptyMemories.gameObject.SetActive(count == 0);
            previousPage.gameObject.SetActive(pages > 1);
            nextPage.gameObject.SetActive(pages > 1);
            previousPage.interactable = memoryPage > 0;
            nextPage.interactable = memoryPage < pages - 1;
            memoryPageLabel.text = count == 0 ? "" : (memoryPage + 1) + " / " + pages;
            Button first = null;
            for (int i = memoryPage * MemoriesPerPage; i < Mathf.Min(count, (memoryPage + 1) * MemoriesPerPage); i++)
            {
                var memory = foundMemories[i];
                var card = CreateButton(memoryCards.transform, memory.Title, 180 - (i % MemoriesPerPage) * 120,
                    () => MemoryRequested?.Invoke(memory.Id));
                ((RectTransform)card.transform).sizeDelta = new Vector2(700, 100);
                var label = card.GetComponentInChildren<Text>();
                label.supportRichText = false;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 26;
                label.rectTransform.sizeDelta = new Vector2(520, 90);
                label.rectTransform.anchoredPosition = new Vector2(70, 0);
                var cover = Center(card.transform, "Cover", 0, new Vector2(120, 90));
                cover.anchoredPosition = new Vector2(-275, 0);
                var image = cover.gameObject.AddComponent<RawImage>();
                image.texture = memory.Cover;
                image.raycastTarget = false;
                if (memory.Cover != null)
                {
                    float ratio = (float)memory.Cover.width / memory.Cover.height;
                    cover.sizeDelta = ratio > 4f / 3 ? new Vector2(120, 120 / ratio) : new Vector2(90 * ratio, 90);
                }
                if (first == null) first = card;
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject((first ?? memoriesBack).gameObject);
        }
    }
}
