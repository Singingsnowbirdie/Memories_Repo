using System;
using System.Collections.Generic;
using UnityEngine;

namespace Memories.Gameplay.Reactions
{
    public sealed class CustomVariableStore
    {
        public const string DefaultKey = "Memories.Progress.CustomVariables.v1";
        [Serializable] private sealed class Entry { public string id; public bool value; public CustomVariableType type; public int intValue; }
        [Serializable] private sealed class SaveData { public List<Entry> entries = new List<Entry>(); }
        private readonly Dictionary<string, Entry> values = new Dictionary<string, Entry>();
        private readonly string saveKey;
        public bool PersistProgress { get; }

        public CustomVariableStore(string saveKey = DefaultKey, bool persistProgress = true)
        {
            this.saveKey = saveKey;
            PersistProgress = persistProgress;
            if (!persistProgress) return;
            if (!PlayerPrefs.HasKey(saveKey)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey));
                if (data?.entries != null)
                    foreach (var entry in data.entries)
                        if (entry != null && !string.IsNullOrEmpty(entry.id)) values[entry.id] = entry;
            }
            catch (ArgumentException) { Debug.LogWarning("Не удалось прочитать сохранение кастомных переменных."); }
        }

        public bool HasSavedValue(CustomVariableBank bank, string id)
        {
            var variable = RequireVariable(bank, id);
            return values.TryGetValue(bank.Id + ":" + id, out var entry) && entry.type == variable.Type;
        }

        public bool Get(CustomVariableBank bank, string id)
        {
            var variable = RequireVariable(bank, id);
            RequireType(variable, CustomVariableType.Boolean);
            return values.TryGetValue(bank.Id + ":" + id, out var value) && value.type == variable.Type ? value.value : variable.InitialValue;
        }

        public void Set(CustomVariableBank bank, string id, bool value)
        {
            RequireType(RequireVariable(bank, id), CustomVariableType.Boolean);
            Save(new Entry { id = bank.Id + ":" + id, value = value, type = CustomVariableType.Boolean });
        }

        public int GetInt(CustomVariableBank bank, string id)
        {
            var variable = RequireVariable(bank, id);
            RequireType(variable, CustomVariableType.Integer);
            return values.TryGetValue(bank.Id + ":" + id, out var entry) && entry.type == variable.Type ? entry.intValue : variable.InitialIntValue;
        }

        public void Set(CustomVariableBank bank, string id, int value)
        {
            RequireType(RequireVariable(bank, id), CustomVariableType.Integer);
            Save(new Entry { id = bank.Id + ":" + id, intValue = value, type = CustomVariableType.Integer });
        }

        private static void RequireType(CustomVariableBank.Variable variable, CustomVariableType type)
        {
            if (variable.Type != type) throw new ArgumentException("Тип кастомной переменной не соответствует операции.");
        }

        private void Save(Entry entry)
        {
            values[entry.id] = entry;
            if (!PersistProgress) return;
            var data = new SaveData();
            foreach (var pair in values) data.entries.Add(pair.Value);
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
