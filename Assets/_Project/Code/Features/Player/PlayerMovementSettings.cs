using System;
using UnityEngine;

namespace Memories.Gameplay.Player
{
    [Serializable]
    public sealed class PlayerMovementSettings
    {
        [Min(0.1f)] public float WalkSpeed = 4f;
        [Min(0.1f)] public float RunSpeed = 7f;
        [Min(0.001f)] public float MouseSensitivity = 0.12f;
        [Range(1f, 89f)] public float PitchLimit = 85f;
        public float Gravity = -20f;
        [Min(1f)] public float TerminalSpeed = 50f;
        [Min(0.5f)] public float Height = 1.8f;
        [Min(0.1f)] public float Radius = 0.3f;
        [Min(0.1f)] public float EyeHeight = 1.65f;
        [Min(0f)] public float StepOffset = 0.3f;
        [Range(0f, 89f)] public float SlopeLimit = 45f;

        public void Validate()
        {
            if (WalkSpeed <= 0 || RunSpeed <= 0 || MouseSensitivity <= 0 || Gravity >= 0 || TerminalSpeed <= 0
                || Height <= 0 || Radius <= 0 || Height < Radius * 2 || EyeHeight <= 0 || EyeHeight > Height
                || StepOffset < 0 || StepOffset > Height || PitchLimit <= 0 || PitchLimit >= 90
                || SlopeLimit < 0 || SlopeLimit >= 90)
                throw new ArgumentException("Invalid player movement settings on AppLifetimeScope.");
        }
    }
}
