using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class StoryTextView : MonoBehaviour, IStoryTextView
    {
        [SerializeField, Min(0)] private float slideDuration = 1.5f;
        [SerializeField, Min(0)] private float initialTextDelay = .7f;
        [SerializeField, Min(0)] private float textFadeDuration = .8f;
        private const float BottomMargin = 0f;
        private const float PanelHeight = 50f;
        private Canvas canvas;
        private RectTransform panel;
        private Text caption;
        private CanvasGroup textGroup;
        private CancellationTokenSource enabledLifetime;
        private int animationVersion;

        public CancellationToken LifetimeToken => enabledLifetime?.Token ?? new CancellationToken(true);

        private void Awake()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var background = new GameObject("Story text panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            background.transform.SetParent(transform, false);
            panel = (RectTransform)background.transform;
            panel.anchorMin = new Vector2(.12f, 0);
            panel.anchorMax = new Vector2(.88f, 0);
            panel.pivot = new Vector2(.5f, 0);
            panel.sizeDelta = new Vector2(0, PanelHeight);
            background.GetComponent<Image>().color = new Color(0, 0, 0, .5f);
            background.GetComponent<Image>().raycastTarget = false;
            var group = background.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var label = new GameObject("Story text", typeof(RectTransform), typeof(CanvasGroup), typeof(Text));
            label.transform.SetParent(panel, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(44, 5);
            rect.offsetMax = new Vector2(-44, -5);
            textGroup = label.GetComponent<CanvasGroup>();
            textGroup.blocksRaycasts = false;
            caption = label.GetComponent<Text>();
            caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.fontSize = 30;
            caption.color = Color.white;
            caption.alignment = TextAnchor.MiddleCenter;
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            caption.supportRichText = false;
            caption.raycastTarget = false;
            HideImmediately();
        }

        private void OnEnable() => enabledLifetime = new CancellationTokenSource();

        private void OnDisable()
        {
            enabledLifetime?.Cancel();
            enabledLifetime?.Dispose();
            enabledLifetime = null;
            HideImmediately();
        }

        public async UniTask ShowAsync(string text, CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, LifetimeToken);
            token = linked.Token;
            token.ThrowIfCancellationRequested();
            int version = ++animationVersion;
            canvas.enabled = true;
            SetText(text);
            textGroup.alpha = 0;
            float hidden = -panel.rect.height - 16;
            panel.anchoredPosition = new Vector2(0, hidden);
            await Animate(slideDuration, t => panel.anchoredPosition = new Vector2(0, Mathf.Lerp(hidden, BottomMargin, t)), version, token);
            await Animate(initialTextDelay, _ => { }, version, token);
            await Animate(textFadeDuration, t => textGroup.alpha = t, version, token);
        }

        public async UniTask ChangeTextAsync(string text, CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, LifetimeToken);
            token = linked.Token;
            token.ThrowIfCancellationRequested();
            int version = ++animationVersion;
            float from = textGroup.alpha;
            if (from > 0)
                await Animate(textFadeDuration, t => textGroup.alpha = Mathf.Lerp(from, 0, t), version, token);
            SetText(text);
            if (!string.IsNullOrEmpty(text))
                await Animate(textFadeDuration, t => textGroup.alpha = t, version, token);
        }

        public async UniTask HideAsync(CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, LifetimeToken);
            token = linked.Token;
            token.ThrowIfCancellationRequested();
            int version = ++animationVersion;
            float from = panel.anchoredPosition.y;
            float hidden = -panel.rect.height - 16;
            await Animate(slideDuration, t => panel.anchoredPosition = new Vector2(0, Mathf.Lerp(from, hidden, t)), version, token);
            HideImmediately();
        }

        private void SetText(string text)
        {
            caption.text = text;
            Canvas.ForceUpdateCanvases();
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, PanelHeight);
        }

        private async UniTask Animate(float duration, Action<float> update, int version, CancellationToken token)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                await UniTask.NextFrame(cancellationToken: token);
                token.ThrowIfCancellationRequested();
                if (version != animationVersion) throw new OperationCanceledException();
                elapsed += Time.deltaTime;
                update(Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration)));
            }
            token.ThrowIfCancellationRequested();
            if (version != animationVersion) throw new OperationCanceledException();
            update(1);
        }

        public void HideImmediately()
        {
            animationVersion++;
            if (canvas == null) return;
            canvas.enabled = false;
            caption.text = string.Empty;
            textGroup.alpha = 0;
            panel.anchoredPosition = new Vector2(0, -panel.rect.height - 16);
        }
    }
}
