namespace Memories.Gameplay.Reactions
{
    /// <summary>Animation that can be stopped explicitly by a reaction graph.</summary>
    public interface IStoppableAnimation
    {
        void StopAnimation();
    }
}
