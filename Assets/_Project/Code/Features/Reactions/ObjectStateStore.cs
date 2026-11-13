using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    // Реализуется компонентами с дополнительным сохраняемым состоянием.
    public interface IObjectStateParticipant
    {
        string StateKey { get; }
        string CaptureState();
        void RestoreState(string state);
    }

    // Компоненты с необязательным состоянием могут указать, нужно ли сохранять эту конфигурацию.
    public interface IConditionalObjectStateParticipant : IObjectStateParticipant
    {
        bool RequiresPersistence { get; }
    }

    public sealed class ObjectStateStore : IDisposable
    {
        public static bool RequiresPersistence(IObjectStateParticipant participant) =>
            !(participant is IConditionalObjectStateParticipant conditional) || conditional.RequiresPersistence;

        public const string DefaultKey = "Memories.Progress.Objects.v1";
        [Serializable] private sealed class Entry { public string id; public string state; }
        [Serializable] private sealed class Data { public List<Entry> entries = new List<Entry>(); }
        private readonly Dictionary<string, string> states = new Dictionary<string, string>();
        private readonly Dictionary<string, ObjectStateTracker> trackers = new Dictionary<string, ObjectStateTracker>();
        private readonly string saveKey;
        private readonly bool persist;
        private bool dirty, stopped;

        public ObjectStateStore(bool persistProgress = true, string saveKey = DefaultKey)
        {
            persist = persistProgress;
            this.saveKey = saveKey;
            if (!persist || !PlayerPrefs.HasKey(saveKey)) return;
            try
            {
                var data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(saveKey));
                if (data?.entries != null)
                    foreach (var entry in data.entries)
                        if (!string.IsNullOrEmpty(entry.id)) states[entry.id] = entry.state;
            }
            catch (ArgumentException) { Debug.LogWarning("Не удалось прочитать состояния объектов."); }
        }

        public void RestoreScene(Scene scene)
        {
            var identities = new Dictionary<string, ObjectStateTracker>(StringComparer.Ordinal);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tracker in root.GetComponentsInChildren<ObjectStateTracker>(true))
                {
                    var id = tracker.PersistentId;
                    ValidateIdentity(tracker.gameObject, id);
                    if (identities.TryGetValue(id, out var other))
                        throw DuplicateIdentity(id, other.gameObject, tracker.gameObject);
                    identities.Add(id, tracker);
                    if (trackers.TryGetValue(id, out var existing) && existing != null && existing != tracker)
                        throw DuplicateIdentity(id, existing.gameObject, tracker.gameObject);
                }
            // Проверяем всю сцену до применения любого сохранённого состояния.
            foreach (var pair in identities) Register(pair.Value.gameObject, pair.Key);
        }

        public static void ValidateIdentity(GameObject target, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                throw new InvalidOperationException("Missing or invalid persistent ID on " + Describe(target));
        }

        private static string Describe(GameObject target)
        {
            string path = target.name;
            for (var parent = target.transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return target.scene.name + ":" + path;
        }

        private static InvalidOperationException DuplicateIdentity(string id, GameObject first, GameObject second) =>
            new InvalidOperationException("Duplicate persistent object ID '" + id + "': " + Describe(first) + " and " + Describe(second));

        public void Register(GameObject target, string id)
        {
            if (stopped) return;
            ValidateIdentity(target, id);
            if (trackers.TryGetValue(id, out var existing) && existing != null)
            {
                if (existing.gameObject != target) throw DuplicateIdentity(id, existing.gameObject, target);
                return;
            }
            var tracker = target.GetComponent<ObjectStateTracker>() ?? target.AddComponent<ObjectStateTracker>();
            trackers[id] = tracker;
            states.TryGetValue(id, out var saved);
            tracker.Initialize(this, id, saved);
        }

        internal void Write(string id, string state)
        {
            if (stopped || (states.TryGetValue(id, out var old) && old == state)) return;
            states[id] = state;
            dirty = true;
        }

        public void Flush()
        {
            if (stopped) return;
            foreach (var tracker in trackers.Values) if (tracker != null) tracker.Capture();
            if (!dirty || !persist) return;
            var data = new Data();
            foreach (var pair in states) data.entries.Add(new Entry { id = pair.Key, state = pair.Value });
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            dirty = false;
        }

        public void Reset()
        {
            stopped = true;
            states.Clear();
            if (persist) { PlayerPrefs.DeleteKey(saveKey); PlayerPrefs.Save(); }
        }
        public void UnloadScene(Scene scene)
        {
            Flush();
            var remove = new List<string>();
            foreach (var pair in trackers)
                if (pair.Value == null || pair.Value.gameObject.scene == scene)
                {
                    if (pair.Value != null) pair.Value.Detach();
                    remove.Add(pair.Key);
                }
            foreach (string key in remove) trackers.Remove(key);
        }
        public void Dispose() { Flush(); stopped = true; }
    }
}
