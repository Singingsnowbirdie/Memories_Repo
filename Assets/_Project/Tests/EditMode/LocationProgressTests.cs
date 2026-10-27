using System;
using Memories.Bootstrap;
using NUnit.Framework;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class LocationProgressTests
    {
        [Test]
        public void HouseCheckpointSurvivesAStoreRecreation()
        {
            string key = "Memories.Tests.Checkpoint." + Guid.NewGuid();
            try
            {
                var store = new LocationProgressStore(key);
                Assert.IsFalse(store.HasReachedHouse);
                store.SaveHouse();
                Assert.IsTrue(new LocationProgressStore(key).HasReachedHouse);
                store.Reset();
                Assert.IsFalse(new LocationProgressStore(key).HasReachedHouse);
                PlayerPrefs.SetInt(key, 99);
                Assert.IsFalse(new LocationProgressStore(key).HasReachedHouse);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }

        [Test]
        public void OnlyCrossingIntoTheDoorwayReachesTheCheckpoint()
        {
            Assert.IsFalse(HouseCheckpointEntryPoint.CrossedEntrance(new Vector3(0, .05f, -8), new Vector3(0, .05f, -1)));
            Assert.IsTrue(HouseCheckpointEntryPoint.CrossedEntrance(new Vector3(0, .05f, -.1f), new Vector3(0, .05f, .1f)));
            Assert.IsFalse(HouseCheckpointEntryPoint.CrossedEntrance(new Vector3(4, .05f, -.1f), new Vector3(4, .05f, .1f)));
            Assert.IsFalse(HouseCheckpointEntryPoint.CrossedEntrance(new Vector3(0, .05f, .1f), new Vector3(0, .05f, -.1f)));
            Assert.IsFalse(HouseCheckpointEntryPoint.CrossedEntrance(new Vector3(0, 4, -.1f), new Vector3(0, 4, .1f)));
        }
    }
}
