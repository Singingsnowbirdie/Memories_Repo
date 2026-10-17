namespace Memories.Gameplay.Memories
{
    public interface IMemoryMusic
    {
        void Execute(MemoryMusicNode node);
        void StopImmediately();
    }
}
