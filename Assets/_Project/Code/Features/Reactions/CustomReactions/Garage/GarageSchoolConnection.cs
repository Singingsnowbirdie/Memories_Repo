using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    // Preloading is explicitly started by the final memory reaction; the gym threshold owns entry.
    public sealed class GarageSchoolConnection : CustomReaction
    {
        [SerializeField] private OpenGarageGateReaction gate;
        private Func<GarageSchoolConnection, UniTask<Scene>> load;
        private UniTaskCompletionSource ready;
        private Scene school;
        public bool IsReady => school.IsValid() && school.isLoaded;

        public void Initialize(Func<GarageSchoolConnection, UniTask<Scene>> loader)
        {
            load = loader;
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

    }
}
