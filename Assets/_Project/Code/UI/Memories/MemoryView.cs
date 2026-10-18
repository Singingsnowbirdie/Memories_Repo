using System.Threading;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using UnityEngine;
using UnityEngine.UI;

namespace Memories.Gameplay.UI
{
    public sealed class MemoryView : MonoBehaviour, IMemoryView
    {
        private sealed class Layer
        {
            public CanvasGroup Group;
            public RawImage Image;
            public Text Text;
        }
        private GameObject panel;
        private Layer front, back;
        private bool hasFrame;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            panel = new GameObject("Memory panel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            Stretch((RectTransform)panel.transform);
            panel.AddComponent<Image>().color = Color.black;
            front = CreateLayer("Current frame");
            back = CreateLayer("Incoming frame");
            Hide();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private Layer CreateLayer(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(panel.transform, false);
            Stretch((RectTransform)root.transform);
            root.AddComponent<Image>().color = Color.black;
            var layer = new Layer { Group = root.GetComponent<CanvasGroup>() };
            layer.Group.blocksRaycasts = false;
            var art = Center(root.transform, "Image", 70, new Vector2(1760, 800));
            layer.Image = art.gameObject.AddComponent<RawImage>();
            layer.Image.raycastTarget = false;
            var textRect = Center(root.transform, "Caption", -440, new Vector2(1640, 140));
            layer.Text = textRect.gameObject.AddComponent<Text>();
            layer.Text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            layer.Text.fontSize = 28;
            layer.Text.alignment = TextAnchor.MiddleCenter;
            layer.Text.raycastTarget = false;
            layer.Text.supportRichText = false;
            return layer;
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

        public void Show()
        {
            ResetLayers();
            panel.SetActive(true);
        }

        public async UniTask ShowFrameAsync(MemoryFrameNode frame, CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, this.GetCancellationTokenOnDestroy());
            token = linked.Token;
            token.ThrowIfCancellationRequested();
            var duration = frame.TransitionDuration;
            SetContent(back, frame);
            back.Group.transform.SetAsLastSibling();
            back.Group.alpha = 0;
            if (frame.Transition == MemoryFrameTransition.Instant || duration <= 0)
            {
                front.Group.alpha = 0;
                back.Group.alpha = 1;
            }
            else if (frame.Transition == MemoryFrameTransition.FadeThroughBlack)
            {
                if (hasFrame) await Fade(front.Group, 1, 0, duration * .5f, token);
                await Fade(back.Group, 0, 1, hasFrame ? duration * .5f : duration, token);
            }
            else
            {
                // Each layer includes its black background and caption.
                await CrossFade(duration, token);
            }
            token.ThrowIfCancellationRequested();
            front.Group.alpha = 0;
            back.Group.alpha = 1;
            var old = front;
            front = back;
            back = old;
            back.Image.texture = null;
            back.Text.text = "";
            hasFrame = true;
        }

        private static void SetContent(Layer layer, MemoryFrameNode frame)
        {
            layer.Image.texture = frame.Image;
            float scale = Mathf.Min(1760f / frame.Image.width, 800f / frame.Image.height);
            layer.Image.rectTransform.sizeDelta = new Vector2(frame.Image.width * scale, frame.Image.height * scale);
            layer.Text.text = frame.Text;
            layer.Text.gameObject.SetActive(!string.IsNullOrEmpty(frame.Text));
            layer.Text.color = Color.white;
        }

        private async UniTask CrossFade(float duration, CancellationToken token)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                await UniTask.NextFrame(cancellationToken: token);
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                back.Group.alpha = t;
            }
        }

        private static async UniTask Fade(CanvasGroup group, float from, float to, float duration, CancellationToken token)
        {
            float elapsed = 0;
            group.alpha = from;
            while (elapsed < duration)
            {
                await UniTask.NextFrame(cancellationToken: token);
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            }
        }

        private void ResetLayers()
        {
            hasFrame = false;
            foreach (var layer in new[] { front, back })
            {
                layer.Group.alpha = 0;
                layer.Image.texture = null;
                layer.Text.text = "";
                layer.Text.color = Color.white;
            }
        }

        public void Hide()
        {
            panel.SetActive(false);
            ResetLayers();
        }
    }
}
