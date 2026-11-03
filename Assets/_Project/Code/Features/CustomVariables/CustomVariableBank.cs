using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public enum CustomVariableType { Boolean = 0, Integer = 1 }
    [CreateAssetMenu(menuName = "Memories/Custom Variable Bank", fileName = "CustomVariables")]
    public sealed class CustomVariableBank : ScriptableObject
    {
        [Serializable]
        public sealed class Variable
        {
            [SerializeField, HideInInspector] private string id;
            [SerializeField] private string name;
            [SerializeField] private bool initialValue;
            [SerializeField] private CustomVariableType type;
            [SerializeField] private int initialIntValue;
            public CustomVariableType Type => type;
            public int InitialIntValue => initialIntValue;
            public string Id => id;
            public string Name => name;
            public bool InitialValue => initialValue;
            public Variable(string name, bool initialValue = false)
            { id = Guid.NewGuid().ToString("N"); this.name = name; this.initialValue = initialValue; }
            public Variable(string name, int initialValue)
            { id = Guid.NewGuid().ToString("N"); this.name = name; type = CustomVariableType.Integer; initialIntValue = initialValue; }
            internal void RenewId() => id = Guid.NewGuid().ToString("N");
        }

        [SerializeField, HideInInspector] private string bankId;
        [SerializeField] private List<Variable> variables = new List<Variable>();
        public string Id => bankId;
        public IReadOnlyList<Variable> Variables => variables;
        public Variable Find(string id) => string.IsNullOrEmpty(id) ? null : variables.FirstOrDefault(v => v != null && v.Id == id);

        private void OnValidate()
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(path)) bankId = UnityEditor.AssetDatabase.AssetPathToGUID(path);
#endif
            if (string.IsNullOrEmpty(bankId)) bankId = Guid.NewGuid().ToString("N");
            var ids = new HashSet<string>();
            foreach (var variable in variables)
            {
                if (variable == null) continue;
                if (string.IsNullOrEmpty(variable.Id) || !ids.Add(variable.Id))
                { variable.RenewId(); ids.Add(variable.Id); }
            }
        }
    }
}
