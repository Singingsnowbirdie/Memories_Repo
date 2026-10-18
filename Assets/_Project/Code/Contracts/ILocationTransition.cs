using Cysharp.Threading.Tasks;

namespace Memories.Contracts
{
    public interface ILocationTransition
    {
        bool CanChangeLocation(string scenePath);
        UniTask ChangeLocationAsync(string scenePath);
    }
}
