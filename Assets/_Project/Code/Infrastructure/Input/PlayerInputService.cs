using System;
using Memories.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Memories.Infrastructure.Input
{
    public sealed class PlayerInputService : IPlayerInput, IInteractionInput, IDisposable
    {
        private readonly InputAction move = new InputAction("Move", InputActionType.Value);
        private readonly InputAction look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        private readonly InputAction release = new InputAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
        private readonly InputAction capture = new InputAction("CaptureCursor", InputActionType.Button, "<Mouse>/leftButton");
        private readonly InputAction interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        public bool InteractionEnabled => active && captured && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
        public bool InteractPressed => InteractionEnabled && Time.frameCount > captureFrame && interact.WasPressedThisFrame();
        private bool active;
        private bool captured;
        private int captureFrame;
        private CursorLockMode previousLock;
        private bool previousVisibility;

        public Vector2 Move => captured ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        public Vector2 LookDelta => captured && Time.frameCount > captureFrame ? look.ReadValue<Vector2>() : Vector2.zero;

        public PlayerInputService()
        {
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Application.focusChanged += OnFocusChanged;
        }

        public void Enable()
        {
            if (active) return;
            previousLock = Cursor.lockState;
            previousVisibility = Cursor.visible;
            active = true;
            release.Enable();
            capture.Enable();
            SetCaptured(Application.isFocused);
        }

        public void Poll()
        {
            if (!active) return;
            if (!Application.isFocused || release.WasPressedThisFrame())
            {
                SetCaptured(false);
                return;
            }
            if (captured && Cursor.lockState != CursorLockMode.Locked) SetCaptured(false);
            if (!captured && capture.WasPressedThisFrame()) SetCaptured(true);
        }

        private void OnFocusChanged(bool focused)
        {
            if (active && !focused) SetCaptured(false);
        }

        private void SetCaptured(bool value)
        {
            captured = value;
            if (value)
            {
                captureFrame = Time.frameCount;
                move.Enable();
                look.Enable();
                interact.Enable();
            }
            else
            {
                move.Disable();
                look.Disable();
                interact.Disable();
            }
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }

        public void Disable()
        {
            if (!active) return;
            SetCaptured(false);
            release.Disable();
            capture.Disable();
            active = false;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisibility;
        }

        public void Dispose()
        {
            Disable();
            Application.focusChanged -= OnFocusChanged;
            move.Dispose();
            look.Dispose();
            release.Dispose();
            capture.Dispose();
            interact.Dispose();
        }
    }
}
