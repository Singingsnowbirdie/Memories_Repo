using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    /// <summary>
    /// Запускается явно из графа реакций, когда требуется открыть или закрыть капот машины.
    /// Мгновенно устанавливает выбранное положение отдельного капота, без анимации и опроса состояния.
    /// SetOpen позволяет явно задать состояние; сохранение восстанавливает его отдельно от игрового перехода.
    /// </summary>
    public sealed class SetCarHoodReaction : CustomReaction, IObjectStateParticipant
    {
        [SerializeField] private Transform hood;
        [SerializeField] private Vector3 closedEuler;
        [SerializeField] private Vector3 openEuler = new Vector3(-68, 0, 0);
        [SerializeField, InspectorName("Открыть при запуске реакции")] private bool openOnExecute = true;
        [SerializeField, InspectorName("Капот открыт")] private bool isOpen;
        public bool IsOpen => isOpen;
        public string StateKey => "car-hood";
        public override void Execute() => SetOpen(openOnExecute);
        public void SetOpen(bool value)
        {
            isOpen = value;
            Apply();
            GetComponent<ObjectStateTracker>()?.Capture();
        }
        [ContextMenu("Открыть капот")] private void Open() => SetOpen(true);
        [ContextMenu("Закрыть капот")] private void Close() => SetOpen(false);
        private void Apply() { if (hood != null) hood.localRotation = Quaternion.Euler(isOpen ? openEuler : closedEuler); }
        private void OnValidate() => Apply();
        public string CaptureState() => isOpen ? "open" : "closed";
        public void RestoreState(string state) { isOpen = state == "open"; Apply(); }
    }
}
