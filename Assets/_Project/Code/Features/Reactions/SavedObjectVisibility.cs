using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // A variable-driven object derives its visibility from that variable, not a cached active flag.
    [DisallowMultipleComponent]
    public sealed class SavedObjectVisibility : MonoBehaviour
    {
        [SerializeField] private CustomVariableBank variableBank;
        [SerializeField] private string variableId;

        public void Restore(CustomVariableStore variables)
        {
            gameObject.SetActive(variables.Get(variableBank, variableId));
        }
    }
}
