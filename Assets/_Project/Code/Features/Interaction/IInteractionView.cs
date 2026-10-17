namespace Memories.Gameplay.Interaction
{
    public interface IInteractionView
    {
        void Show(bool visible, bool available, string prompt);
    }
}
