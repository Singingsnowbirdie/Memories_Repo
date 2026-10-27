using UnityEngine;

namespace Memories.Bootstrap
{
    public sealed class LocationProgressStore
    {
        private readonly string key;
        public LocationProgressStore(string key = "Memories.Progress.Checkpoint.v1") => this.key = key;
        public bool HasReachedHouse => PlayerPrefs.GetInt(key, 0) == 1;

        public void SaveHouse()
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
    }
}
