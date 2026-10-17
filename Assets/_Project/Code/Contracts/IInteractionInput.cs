namespace Memories.Contracts
{
    public interface IInteractionInput
    {
        bool InteractionEnabled { get; }
        bool InteractPressed { get; }
    }
}
