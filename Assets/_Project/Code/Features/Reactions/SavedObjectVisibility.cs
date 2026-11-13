using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    // Видимость объекта, управляемого переменной, определяется этой переменной, а не сохранённым флагом активности.
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
