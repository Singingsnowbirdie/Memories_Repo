using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    // Preloading is explicitly started by the final memory reaction. Update only detects crossing.
    public sealed class GarageSchoolConnection : CustomReaction
    {
        [SerializeField] private OpenGarageGateReaction gate;
        private Func<GarageSchoolConnection, UniTask<Scene>> load;
        private Func<Scene, UniTask> enter;
        private PlayerMotor player;
        private UniTaskCompletionSource ready;
        private Scene school;
        private bool crossing;
        public bool IsReady => school.IsValid() && school.isLoaded;

        public void Initialize(PlayerMotor motor, Func<GarageSchoolConnection, UniTask<Scene>> loader,
            Func<Scene, UniTask> traveller)
        {
            player = motor;
            load = loader;
            enter = traveller;
            gate.Initialize(EnsureReadyAsync);
        }

        public override void Execute()
        {
            if (load == null) return;
            EnsureReadyAsync().Forget(exception =>
            {
                if (!(exception is OperationCanceledException)) Debug.LogException(exception, this);
            });
        }

        public UniTask EnsureReadyAsync()
        {
            if (IsReady) return UniTask.CompletedTask;
            if (ready != null) return ready.Task;
            if (load == null) return UniTask.FromException(new InvalidOperationException("School connection is not initialized."));
            var completion = new UniTaskCompletionSource();
            ready = completion;
            LoadAsync(completion).Forget();
            return completion.Task;
        }

        private async UniTask LoadAsync(UniTaskCompletionSource completion)
        {
            try
            {
                school = await load(this);
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
                ready = null;
            }
        }

        private void LateUpdate()
        {
            if (!IsReady || !gate.IsOpen || crossing || player == null) return;
            var local = transform.InverseTransformPoint(player.transform.position);
            if (local.z < 2f || local.z > 5f || Mathf.Abs(local.x) > 2.3f || Mathf.Abs(local.y) > 3f) return;
            crossing = true;
            EnterAsync().Forget();
        }

        private async UniTask EnterAsync()
        {
            try { await enter(school); }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                if (this != null) { crossing = false; Debug.LogException(exception, this); }
            }
        }
    }
}
