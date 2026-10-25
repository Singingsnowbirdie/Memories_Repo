using Memories.Contracts;
using Memories.Gameplay.Player;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class PlayerPresenterTests
    {
        private sealed class InputStub : IPlayerInput
        {
            public Vector2 Move { get; set; }
            public bool SprintHeld { get; set; }
            public Vector2 LookDelta { get; set; }
            public bool Enabled;
            public void Enable() => Enabled = true;
            public void Disable() => Enabled = false;
            public void Poll() { }
        }

        private sealed class ViewStub : IPlayerView
        {
            public float InitialYaw { get; set; }
            public bool IsGrounded => true;
            public Vector3 Position;
            public float Yaw;
            public float Pitch;
            public void SetLook(float yaw, float pitch) { Yaw = yaw; Pitch = pitch; }
            public CollisionFlags Move(Vector3 displacement) { Position += displacement; return CollisionFlags.Below; }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void DiagonalMovementHasWalkingSpeedAtDifferentFrameRates(int frames)
        {
            var settings = new PlayerMovementSettings();
            var input = new InputStub { Move = Vector2.one };
            var view = new ViewStub();
            using var presenter = new PlayerPresenter(input, new PlayerMovementModel(settings), view, settings);
            presenter.Start();
            for (int i = 0; i < frames; i++) presenter.Tick(1f / frames);
            Assert.That(new Vector2(view.Position.x, view.Position.z).magnitude, Is.EqualTo(settings.WalkSpeed).Within(0.001f));
        }

        [TestCase(30)]
        [TestCase(144)]
        public void MouseDeltaIsNotScaledByFrameTimeAndPitchIsLimited(int frames)
        {
            var settings = new PlayerMovementSettings();
            var input = new InputStub { LookDelta = new Vector2(100f / frames, 100000f / frames) };
            var view = new ViewStub();
            using var presenter = new PlayerPresenter(input, new PlayerMovementModel(settings), view, settings);
            presenter.Start();
            for (int i = 0; i < frames; i++) presenter.Tick(1f / frames);
            Assert.That(view.Yaw, Is.EqualTo(12f).Within(0.001f));
            Assert.That(view.Pitch, Is.EqualTo(-settings.PitchLimit));
        }

        [Test]
        public void DisabledControlsBlockMovementAndLookUntilReleased()
        {
            var settings = new PlayerMovementSettings();
            var input = new InputStub { Move = Vector2.up, LookDelta = new Vector2(10, 10) };
            var view = new ViewStub { InitialYaw = 30 };
            using var presenter = new PlayerPresenter(input, new PlayerMovementModel(settings), view, settings);
            presenter.ControlsEnabled = false;
            presenter.Start();
            for (int i = 0; i < 60; i++) presenter.Tick(1f / 60);
            Assert.AreEqual(0, view.Position.x);
            Assert.AreEqual(0, view.Position.z);
            Assert.AreEqual(30, view.Yaw);
            Assert.AreEqual(0, view.Pitch);
            Assert.IsTrue(input.Enabled, "Pause input must remain available during the introduction.");

            presenter.ControlsEnabled = true;
            presenter.Tick(1f / 60);
            Assert.Greater(new Vector2(view.Position.x, view.Position.z).magnitude, 0);
            Assert.Greater(view.Yaw, 30);
            Assert.Less(view.Pitch, 0);
        }

        [Test]
        public void MovementFollowsSpawnYawAndStopsAfterDisposal()
        {
            var settings = new PlayerMovementSettings();
            var input = new InputStub { Move = Vector2.up };
            var view = new ViewStub { InitialYaw = 90f };
            var presenter = new PlayerPresenter(input, new PlayerMovementModel(settings), view, settings);
            presenter.Start();
            presenter.Tick(1);
            Assert.That(view.Position.x, Is.EqualTo(4).Within(0.001f));
            Assert.That(view.Position.z, Is.EqualTo(0).Within(0.001f));
            presenter.Dispose();
            var stopped = view.Position;
            presenter.Tick(1);
            Assert.IsFalse(input.Enabled);
            Assert.AreEqual(stopped, view.Position);
        }
    }
}
