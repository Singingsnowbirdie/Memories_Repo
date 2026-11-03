using Memories.Gameplay.Memories;
using UnityEngine;
namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается графом Baseball_PlayReaction после второго воспоминания о разбитом окне.
    /// Отключает Бена-питчера и показывает сидящего под деревом Бена в кепке, с битой и мячом рядом.
    /// При загрузке локации однократно восстанавливает этот результат по просмотренному воспоминанию.
    /// </summary>
    public sealed class ShowSeatedBenReaction : CustomReaction
    {
        [SerializeField] private GameObject pitcher;
        [SerializeField] private GameObject seatedBen;
        public void Restore(MemoryCollection memories)
        {
            if (memories.Contains(BackyardGarageAttachment.MemoryId)) Execute();
            else seatedBen.SetActive(false);
        }
        public override void Execute()
        {
            pitcher.SetActive(false);
            seatedBen.SetActive(true);
        }
    }
}
