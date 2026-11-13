using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.Memories;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class SchoolAmbulanceDepartureReaction : CustomReaction
    {
        [SerializeField] private GameObject doctor;
        private Func<UniTask> loadPark;
        private MemoryPresenter memories;
        public override bool TransfersMemoryOwnership => true;

        public void Initialize(MemoryPresenter presenter, Func<UniTask> loader)
        {
            memories = presenter;
            loadPark = loader;
        }

        public override void Execute()
        {
            if (loadPark == null) throw new InvalidOperationException("Park transition is not initialized.");
            memories.DeferCompletion(loadPark());
            doctor.SetActive(false);
            doctor.GetComponent<ObjectStateTracker>()?.Capture();
            gameObject.SetActive(false);
            GetComponent<ObjectStateTracker>()?.Capture();
        }
    }
}
