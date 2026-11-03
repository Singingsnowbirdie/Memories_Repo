using UnityEngine;

namespace Memories.Gameplay.Interaction
{
    [CreateAssetMenu(menuName = "Memories/Quest Item")]
    public sealed class QuestItem : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public bool IsValid => !string.IsNullOrWhiteSpace(id);

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N");
        }
    }
}
