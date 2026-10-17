using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class MemoryView : MonoBehaviour, IMemoryView
    {
        public event Action CloseRequested;
        public event Action AdvanceRequested;
        public bool IsTransitioning { get; private set; }
        private GameObject panel;
        private Text heading;
        private Text body;
        private Button close;
        private Text buttonLabel;
        private RawImage illustration;
        private CancellationTokenSource transition;
        private bool showingComic;

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
            panel.AddComponent<Image>().color = Color.black;
            var artRect = Center("Comic frame", 50, new Vector2(1760, 880));
            illustration = artRect.gameObject.AddComponent<RawImage>();
            illustration.raycastTarget = false;
            heading = MakeText("Title", 40, 150, 90);
            body = MakeText("Text", 28, 0, 180);
            var buttonRect = Center("Return", -465, new Vector2(420, 65));
            var background = buttonRect.gameObject.AddComponent<Image>();
            background.color = new Color(.22f, .25f, .3f);
            close = buttonRect.gameObject.AddComponent<Button>();
            close.targetGraphic = background;
            close.onClick.AddListener(() =>
            {
                if (IsTransitioning) return;
                if (showingComic) AdvanceRequested?.Invoke();
                else CloseRequested?.Invoke();
            });
            buttonLabel = MakeText("Return label", 26, -465, 65);
            buttonLabel.text = "Вернуться";
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
            CancelTransition();
            showingComic = false;
            illustration.texture = null;
            illustration.gameObject.SetActive(false);
            close.interactable = true;
            buttonLabel.text = "Вернуться";
            heading.text = title;
            body.text = text;
            panel.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
        }

        public void Hide()
        {
            CancelTransition();
            panel.SetActive(false);
            illustration.texture = null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        public void ShowFrame(Texture2D frame, bool isLast)
        {
            CancelTransition();
            showingComic = true;
            buttonLabel.text = isLast ? "Вернуться" : "Далее";
            illustration.gameObject.SetActive(true);
            transition = new CancellationTokenSource();
            FadeFrame(frame, transition.Token).Forget();
        }

        private async UniTask FadeFrame(Texture2D frame, CancellationToken token)
        {
            IsTransitioning = true;
            close.interactable = false;
            try
            {
                if (illustration.texture != null) await Fade(1, 0, token);
                token.ThrowIfCancellationRequested();
                illustration.texture = frame;
                float ratio = (float)frame.width / frame.height;
                illustration.rectTransform.sizeDelta = ratio > 2f
                    ? new Vector2(1760, 1760 / ratio) : new Vector2(880 * ratio, 880);
                await Fade(0, 1, token);
                IsTransitioning = false;
                close.interactable = true;
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask Fade(float from, float to, CancellationToken token)
        {
            const float duration = .25f;
            float elapsed = 0;
            illustration.color = new Color(1, 1, 1, from);
            while (elapsed < duration)
            {
                await UniTask.NextFrame(cancellationToken: token);
                elapsed += Time.unscaledDeltaTime;
                illustration.color = new Color(1, 1, 1, Mathf.Lerp(from, to, elapsed / duration));
            }
        }

        private void CancelTransition()
        {
            transition?.Cancel();
            transition?.Dispose();
            transition = null;
            IsTransitioning = false;
        }

        private void OnDestroy() => CancelTransition();
    }
}
