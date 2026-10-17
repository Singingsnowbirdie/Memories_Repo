using Memories.Gameplay.Memories;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Memories.Tests.EditMode
{
    public sealed class MemoryComicImportTests
    {
        [Test]
        public void ComicFramesImportWithColorChannels()
        {
            var comic = Resources.Load<MemoryComic>("Memories/FirstMemoryComic");
            Assert.IsNotNull(comic);
            Assert.AreEqual(3, comic.FrameCount);
            for (int i = 0; i < comic.FrameCount; i++)
            {
                var frame = comic.GetFrame(i);
                Assert.IsNotNull(frame);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frame));
                Assert.AreEqual(TextureImporterType.Default, importer.textureType, frame.name);
                Assert.That(frame.format, Is.EqualTo(TextureFormat.RGB24).Or.EqualTo(TextureFormat.RGBA32),
                    frame.name + " must retain RGB channels, not import as Alpha8.");
                Assert.Greater(frame.width, 1000);
                Assert.Greater(frame.height, 500);
            }
        }
    }
}
