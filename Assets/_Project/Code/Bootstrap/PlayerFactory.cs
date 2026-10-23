using System;
using Memories.Gameplay.Player;
using UnityEngine;

namespace Memories.Bootstrap
{
    internal static class PlayerFactory
    {
        public static PlayerMotor Create(LocationLifetimeScope scope, PlayerMovementSettings settings)
        {
            settings.Validate();
            Transform spawn = null;
            Camera viewCamera = null;
            foreach (var root in scope.gameObject.scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                    if (candidate.name == "PlayerSpawn")
                    {
                        if (spawn != null) throw new InvalidOperationException("Multiple PlayerSpawn points in location.");
                        spawn = candidate;
                    }
                foreach (var candidate in root.GetComponentsInChildren<Camera>())
                    if (candidate.CompareTag("MainCamera"))
                    {
                        if (viewCamera != null) throw new InvalidOperationException("Multiple MainCamera cameras in location.");
                        viewCamera = candidate;
                    }
            }
            if (spawn == null || viewCamera == null)
                throw new InvalidOperationException("Location requires one PlayerSpawn and one active MainCamera.");

            viewCamera.enabled = true;
            var listener = viewCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;

            var player = new GameObject("Player");
            player.SetActive(false);
            player.transform.SetParent(scope.transform, false);
            player.transform.SetPositionAndRotation(spawn.position, Quaternion.Euler(0, spawn.eulerAngles.y, 0));
            var motor = player.AddComponent<PlayerMotor>();
            motor.Initialize(settings, viewCamera.transform);
            player.SetActive(true);
            return motor;
        }
    }
}
