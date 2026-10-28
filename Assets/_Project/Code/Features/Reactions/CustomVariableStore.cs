using System;
using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class CustomVariableStore
    {
        public const string DefaultKey = "Memories.Progress.CustomVariables.v1";
        [Serializable] private sealed class Entry { public string id; public bool value; }
        [Serializable] private sealed class SaveData { public List<Entry> entries = new List<Entry>(); }
        private readonly Dictionary<string, bool> values = new Dictionary<string, bool>();
        private readonly string saveKey;

        public CustomVariableStore(string saveKey = DefaultKey)
        {
            this.saveKey = saveKey;
            if (!PlayerPrefs.HasKey(saveKey)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey));
                if (data?.entries != null)
                    foreach (var entry in data.entries)
                        if (entry != null && !string.IsNullOrEmpty(entry.id)) values[entry.id] = entry.value;
            }
            catch (ArgumentException) { Debug.LogWarning("Не удалось прочитать сохранение кастомных переменных."); }
        }

        public bool HasSavedValue(CustomVariableBank bank, string id)
        {
            RequireVariable(bank, id);
            return values.ContainsKey(bank.Id + ":" + id);
        }

        public bool Get(CustomVariableBank bank, string id)
        {
            var variable = RequireVariable(bank, id);
            return values.TryGetValue(bank.Id + ":" + id, out var value) ? value : variable.InitialValue;
        }

        public void Set(CustomVariableBank bank, string id, bool value)
        {
            RequireVariable(bank, id);
            values[bank.Id + ":" + id] = value;
            var data = new SaveData();
            foreach (var pair in values) data.entries.Add(new Entry { id = pair.Key, value = pair.Value });
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private static CustomVariableBank.Variable RequireVariable(CustomVariableBank bank, string id)
        {
            var variable = bank != null ? bank.Find(id) : null;
            return variable ?? throw new ArgumentException("Кастомная переменная не найдена в банке.");
        }

        public static void Clear(string saveKey = DefaultKey)
        { PlayerPrefs.DeleteKey(saveKey); PlayerPrefs.Save(); }
    }
}
