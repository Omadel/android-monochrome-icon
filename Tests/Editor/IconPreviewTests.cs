using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class IconPreviewTests
    {
        private static readonly Color32 _opaque = new Color32(255, 255, 255, 255);
        private static readonly Color32 _clear = new Color32(255, 255, 255, 0);

        [Test]
        public void SampleVisibleLayer_OpaqueOuterSixth_IsHidden()
        {
            var pixels = Image(6, (x, y) => x == 0 || y == 0 || x == 5 || y == 5);
            Assert.That(IconPreview.SampleVisibleLayer(pixels, 6, 6, 4), Is.All.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SampleVisibleLayer_TopVisibleRow_ComesFirst()
        {
            var pixels = Image(6, (x, y) => y == 1);
            var alpha = IconPreview.SampleVisibleLayer(pixels, 6, 6, 4);
            Assert.That(alpha.Take(4), Is.All.EqualTo(1f).Within(1e-4f));
            Assert.That(alpha.Skip(4), Is.All.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SampleVisibleLayer_SmallerGrid_AveragesPixels()
        {
            var pixels = Image(12, (x, y) => (x + y) % 2 == 0);
            Assert.That(IconPreview.SampleVisibleLayer(pixels, 12, 12, 4), Is.All.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Compose_MaskAndLayer_GivesShapeColoursAndTextureRowOrder()
        {
            var background = new Color32(200, 210, 255, 255);
            var foreground = new Color32(50, 70, 110, 255);
            var pixels = IconPreview.Compose(new[] { 0f, 1f, 1f, 0.5f }, new[] { 1f, 0f, 1f, 1f }, 2, background, foreground);
            Assert.That(pixels[2].a, Is.EqualTo(0));
            Assert.That(pixels[3], Is.EqualTo(background));
            Assert.That(pixels[0], Is.EqualTo(foreground));
            Assert.That(pixels[1], Is.EqualTo(new Color32(50, 70, 110, 128)));
        }

        /// <summary>
        ///     Pixels of a square texture, rows from the bottom, opaque where the condition holds for a pixel counted from
        ///     the top.
        /// </summary>
        private static Color32[] Image(int size, Func<int, int, bool> isOpaque)
        {
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[(size - 1 - y) * size + x] = isOpaque(x, y) ? _opaque : _clear;
                }
            }

            return pixels;
        }
    }
}