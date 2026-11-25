using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class LayerPixelsTests
    {
        private static readonly Color32 _white = new Color32(255, 255, 255, 255);
        private static readonly Color32 _clear = new Color32(255, 255, 255, 0);
        private static readonly Color32 _black = new Color32(0, 0, 0, 255);
        private static readonly Color32 _grey = new Color32(128, 128, 128, 255);

        [Test]
        public void Opacity_White_KeepsOpacity()
        {
            Assert.That(new byte[] { 0, 1, 128, 255 }.Select(alpha => LayerPixels.Opacity(new Color32(255, 255, 255, alpha))), Is.EqualTo(new byte[] { 0, 1, 128, 255 }));
        }

        [Test]
        public void Opacity_Grey_ScalesOpacityByGreyLevel()
        {
            Assert.That(LayerPixels.Opacity(_grey), Is.EqualTo(128));
            Assert.That(LayerPixels.Opacity(new Color32(128, 128, 128, 128)), Is.EqualTo(64));
        }

        [Test]
        public void Opacity_Black_IsZero()
        {
            Assert.That(LayerPixels.Opacity(_black), Is.EqualTo(0));
        }

        [Test]
        public void FromImage_Pixels_GivesWhiteWithTheirOpacityInOrder()
        {
            var layer = LayerPixels.FromImage(new[] { _white, _grey, _black, _clear });
            Assert.That(layer, Is.EqualTo(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 128), new Color32(255, 255, 255, 0), new Color32(255, 255, 255, 0) }));
        }

        [Test]
        public void IsWhite_WhiteAndTransparentPixels_ReturnsTrue()
        {
            Assert.That(LayerPixels.IsWhite(new[] { _white, _clear, new Color32(0, 0, 0, 0), new Color32(255, 255, 255, 128) }), Is.True);
        }

        [Test]
        public void IsWhite_VisibleGreyPixel_ReturnsFalse()
        {
            Assert.That(LayerPixels.IsWhite(new[] { _white, new Color32(254, 254, 254, 1) }), Is.False);
        }

        [Test]
        public void IsEmpty_BlackAndTransparentPixels_ReturnsTrue()
        {
            Assert.That(LayerPixels.IsEmpty(new[] { _black, _clear, new Color32(0, 0, 0, 128) }), Is.True);
        }

        [Test]
        public void IsEmpty_OneGreyPixel_ReturnsFalse()
        {
            Assert.That(LayerPixels.IsEmpty(new[] { _black, _clear, _grey }), Is.False);
        }

        [Test]
        public void ToLayerFile_WhiteImage_ReturnsFileAsItIs()
        {
            var file = Png(2, 2, _white, _clear, new Color32(255, 255, 255, 128), _white);
            Assert.That(LayerPixels.ToLayerFile(file), Is.SameAs(file));
        }

        [Test]
        public void ToLayerFile_GreyImage_ReturnsWhitePngWithGreyLevelsAsOpacity()
        {
            var layer = LayerPixels.ToLayerFile(Png(2, 2, _white, _grey, _black, new Color32(128, 128, 128, 128)));
            Assert.That(ImageChecks.IsPng(layer), Is.True);
            Assert.That(ImageChecks.TryReadPixels(layer, out var width, out var height, out var pixels), Is.True);
            Assert.That(new[] { width, height }, Is.EqualTo(new[] { 2, 2 }));
            Assert.That(pixels, Is.EqualTo(new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 128), new Color32(255, 255, 255, 0), new Color32(255, 255, 255, 64) }));
        }

        [Test]
        public void ToLayerFile_NotPng_ReturnsNull()
        {
            Assert.That(LayerPixels.ToLayerFile(new byte[] { 1, 2, 3 }), Is.Null);
        }

        /// <summary>
        ///     PNG file of these pixels, rows from the bottom as a texture takes them.
        /// </summary>
        private static byte[] Png(int width, int height, params Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}