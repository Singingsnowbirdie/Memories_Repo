using UnityEngine;

namespace Memories.Bootstrap
{
    public sealed class LocationProgressStore
    {
        private readonly string key;
        public LocationProgressStore(string key = "Memories.Progress.Checkpoint.v1") => this.key = key;
        public bool HasReachedHouse => PlayerPrefs.GetInt(key, 0) == 1;
        public string ScenePath => PlayerPrefs.GetString(key + ".Scene",
            HasReachedHouse ? TestLocationLauncher.HousePath : string.Empty);
        public TesterCheckpoint? Checkpoint => PlayerPrefs.HasKey(key + ".Point") &&
            System.Enum.IsDefined(typeof(TesterCheckpoint), PlayerPrefs.GetInt(key + ".Point"))
                ? (TesterCheckpoint)PlayerPrefs.GetInt(key + ".Point") : null;

        public void SaveCheckpoint(string scenePath, TesterCheckpoint checkpoint)
        {
            SaveLocation(scenePath);
            PlayerPrefs.SetInt(key + ".Point", (int)checkpoint);
            PlayerPrefs.Save();
        }

        public void SaveLocation(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath)) throw new System.ArgumentException(nameof(scenePath));
            PlayerPrefs.SetString(key + ".Scene", scenePath);
            PlayerPrefs.DeleteKey(key + ".Point");
            PlayerPrefs.Save();
        }

        public void Reset()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + ".Scene");
            PlayerPrefs.DeleteKey(key + ".Point");
            PlayerPrefs.Save();
        }

        public void SaveHouse()
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.SetString(key + ".Scene", TestLocationLauncher.HousePath);
            PlayerPrefs.Save();
        }
    }
}
