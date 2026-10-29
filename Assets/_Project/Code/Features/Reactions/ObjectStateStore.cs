using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memories.Gameplay.Reactions
{
    // Implemented by code when a component has additional persistent state.
    public interface IObjectStateParticipant
    {
        string StateKey { get; }
        string CaptureState();
        void RestoreState(string state);
    }

    public sealed class ObjectStateStore : IDisposable
    {
        public const string DefaultKey = "Memories.Progress.Objects.v1";
        [Serializable] private sealed class Entry { public string id; public string state; }
        [Serializable] private sealed class Data { public List<Entry> entries = new List<Entry>(); }
        private readonly Dictionary<string, string> states = new Dictionary<string, string>();
        private readonly Dictionary<string, ObjectStateTracker> trackers = new Dictionary<string, ObjectStateTracker>();
        private readonly string saveKey;
        private readonly bool persist;
        private bool dirty, stopped;
        public static Func<GameObject, string> EditorIdentity;

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
            var identities = new Dictionary<GameObject, string>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var map in root.GetComponentsInChildren<SceneObjectIds>(true))
                    for (int i = 0; i < map.objects.Length; i++)
                        if (map.objects[i] != null) identities[map.objects[i]] = map.ids[i];
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var target = transform.gameObject;
                    if (!identities.TryGetValue(target, out var id)) id = EditorIdentity?.Invoke(target);
                    if (!string.IsNullOrEmpty(id)) Register(target, id);
                }
        }

        public void Register(GameObject target, string id)
        {
            if (stopped) return;
            if (trackers.TryGetValue(id, out var existing) && existing != null)
            {
                if (existing.gameObject != target) throw new InvalidOperationException("Duplicate persistent object ID: " + id);
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
