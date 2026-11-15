using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using Memories.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    public sealed class SchoolAmbulanceDepartureReaction : CustomReaction
    {
        [SerializeField] private GameObject doctor;
        private Func<UniTask> loadPark;
        private MemoryPresenter memories;
        private PlayerMotor player;
        private PlayerMovementModel movement;
        public override bool TransfersMemoryOwnership => true;

        public void Initialize(MemoryPresenter presenter, Func<UniTask> loader,
            PlayerMotor player, PlayerMovementModel movement)
        {
            memories = presenter;
            loadPark = loader;
            this.player = player;
            this.movement = movement;
        }

        public override void Execute()
        {
            if (loadPark == null) throw new InvalidOperationException("Park transition is not initialized.");
            ArriveInParkAsync(loadPark, player, movement).Forget(Debug.LogException);
            doctor.SetActive(false);
            doctor.GetComponent<ObjectStateTracker>()?.Capture();
            gameObject.SetActive(false);
            GetComponent<ObjectStateTracker>()?.Capture();
        }

        private static async UniTask ArriveInParkAsync(Func<UniTask> loader,
            PlayerMotor player, PlayerMovementModel movement)
        {
            // The school and this reaction are unloaded by the transition.
            await loader();
            if (player == null) return;
            var park = SceneManager.GetSceneByPath("Assets/_Project/Locations/Park/Park.unity");
            if (!park.IsValid() || !park.isLoaded) return;
            foreach (var root in park.GetRootGameObjects())
                foreach (var marker in root.GetComponentsInChildren<Transform>(true))
                    if (marker.name == "PlayerSpawn")
                    {
                        float yaw = marker.eulerAngles.y;
                        player.Teleport(marker.position, yaw);
                        movement.ResetMotion(yaw);
                        return;
                    }
            throw new InvalidOperationException("Park arrival requires a PlayerSpawn marker.");
        }
    }
}
