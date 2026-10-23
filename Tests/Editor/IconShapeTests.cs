using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class IconShapeTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "AndroidMonochromeIconTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_folder, true);
        }

        [Test]
        public void Find_Folder_ListsPngFilesWithSubfoldersByName()
        {
            Directory.CreateDirectory(Path.Combine(_folder, "Sub"));
            foreach (var file in new[] { "b.png", "A.png", Path.Combine("Sub", "c.PNG"), "notes.txt", "b.png.meta" })
            {
                File.WriteAllBytes(Path.Combine(_folder, file), new byte[0]);
            }

            Assert.That(IconShape.Find(_folder).Select(shape => shape.Name), Is.EqualTo(new[] { "A", "b", "c" }));
        }

        [Test]
        public void Find_MissingFolder_ReturnsNoShape()
        {
            Assert.That(IconShape.Find(Path.Combine(_folder, "Missing")), Is.Empty);
        }

        [Test]
        public void Mask_NotPng_ReturnsNull()
        {
            Assert.That(IconShape.Mask(new byte[] { 1, 2, 3 }, 4), Is.Null);
        }

        [Test]
        public void Mask_Png_ReturnsOpacityOfWholeImage()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels32(Enumerable.Range(0, 16).Select(i => new Color32(255, 255, 255, (byte)(i % 4 < 2 ? 255 : 0))).ToArray());
            var png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            Assert.That(IconShape.Mask(png, 2), Is.EqualTo(new[] { 1f, 0f, 1f, 0f }).Within(1e-4f));
        }

        [Test]
        public void PackageShapes_AllFourteen_AreSquareReadableMasks()
        {
            var shapes = IconShape.Find(IconShape.PackageFolder);
            Assert.That(shapes.Count, Is.EqualTo(14));
            foreach (var shape in shapes)
            {
                Assert.That(ImageChecks.TryReadPixels(File.ReadAllBytes(shape.FilePath), out var width, out var height, out _), Is.True, shape.Name);
                Assert.That(width, Is.EqualTo(height), shape.Name);
                Assert.That(shape.LoadMask(32).Average(), Is.InRange(0.7f, 1f), shape.Name);
            }
        }

        [Test]
        public void PackageShapes_Circle_CoversItsArea()
        {
            var circle = IconShape.Find(IconShape.PackageFolder).Single(shape => shape.Name.StartsWith("Circle"));
            Assert.That(circle.LoadMask(96).Average(), Is.EqualTo(Mathf.PI / 4).Within(0.5).Percent);
        }
    }
}