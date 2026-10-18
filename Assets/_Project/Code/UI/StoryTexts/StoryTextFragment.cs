using System;
using UnityEngine;

namespace Memories.Gameplay.UI
{
    [Serializable]
    public sealed class StoryTextFragment
    {
        [SerializeField, TextArea(2, 6)] private string text;
        [SerializeField, Min(.1f)] private float duration = 3f;

        public string Text => text;
        public float Duration => duration;

        public StoryTextFragment(string text, float duration = 3f)
        {
            this.text = text;
            this.duration = duration;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("A story text fragment must contain text.");
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration), "Display duration must be finite and positive.");
        }
    }
}
