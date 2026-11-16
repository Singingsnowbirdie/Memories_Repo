using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Memories.Contracts
{
    public interface ILocationTransition
    {
        bool CanLoadSegment(string scenePath);
        UniTask LoadSegmentAsync(string scenePath, Transform connection, string arrivalMarker);
        bool CanSwitchEnvironment(string scenePath);
        UniTask SwitchEnvironmentAsync(string scenePath);
        bool CanChangeLocation(string scenePath);
        UniTask ChangeLocationAsync(string scenePath);
    }
}
