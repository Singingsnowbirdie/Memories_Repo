using UnityEngine;

namespace Memories.Gameplay.Player
{
    public sealed class CursorSettings
    {
        // Options are independent of session progress and survive save resets.
        private const string Prefix = "Memories.Options.Cursor.";
        public bool InvertX { get; private set; } = PlayerPrefs.GetInt(Prefix + "InvertX", 0) != 0;
        public bool InvertY { get; private set; } = PlayerPrefs.GetInt(Prefix + "InvertY", 0) != 0;

        public void SetInvertX(bool value) { InvertX = value; Save("InvertX", value); }
        public void SetInvertY(bool value) { InvertY = value; Save("InvertY", value); }
        public Vector2 Apply(Vector2 delta) => new Vector2(InvertX ? -delta.x : delta.x, InvertY ? -delta.y : delta.y);

        private static void Save(string axis, bool value)
        {
            PlayerPrefs.SetInt(Prefix + axis, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
