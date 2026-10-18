using Memories.Gameplay.Memories;
using Memories.Gameplay.UI;
using UnityEngine;

namespace Memories.Gameplay.Triggers
{
    public abstract class TriggerEvent : MonoBehaviour
    {
        public abstract void Initialize(StoryTextPresenter texts, MemoryPresenter memories);
        public abstract bool TryExecute();
    }
}
