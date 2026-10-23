using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class ShapeMaskTests
    {
        [Test]
        public void Rasterize_FullSquare_CoversEveryPixel()
        {
            Assert.That(ShapeMask.Rasterize(Rectangle(0, 0, 100, 100), 8, 100), Is.All.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Rasterize_LeftHalf_CoversLeftColumnsOnly()
        {
            var mask = ShapeMask.Rasterize(Rectangle(0, 0, 50, 100), 10, 100);
            Assert.That(Row(mask, 10, 3), Is.EqualTo(new[] { 1f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 0f }).Within(1e-4f));
        }

        [Test]
        public void Rasterize_TopHalf_CoversFirstRowsOnly()
        {
            var mask = ShapeMask.Rasterize(Rectangle(0, 0, 100, 50), 10, 100);
            Assert.That(Enumerable.Range(0, 10).Select(row => mask[row * 10]), Is.EqualTo(new[] { 1f, 1f, 1f, 1f, 1f, 0f, 0f, 0f, 0f, 0f }).Within(1e-4f));
        }

        [Test]
        public void Rasterize_EdgeInsidePixel_CoversItPartly()
        {
            var mask = ShapeMask.Rasterize(Rectangle(0, 0, 55, 100), 10, 100);
            Assert.That(mask[5], Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Rasterize_Circle_CoversItsArea()
        {
            var mask = ShapeMask.Rasterize(PathData.Flatten("M50 0A50 50,0,1,1,50 100A50 50,0,1,1,50 0"), 64, 100);
            Assert.That(mask.Sum() / (64 * 64), Is.EqualTo(Mathf.PI / 4).Within(0.5).Percent);
        }

        [Test]
        public void Rasterize_SameSquareTwice_StaysCoveredWithNonZeroRule()
        {
            var polygons = Rectangle(0, 0, 100, 100);
            polygons.AddRange(Rectangle(0, 0, 100, 100));
            Assert.That(ShapeMask.Rasterize(polygons, 4, 100), Is.All.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Rasterize_InnerSquareInOppositeDirection_LeavesHole()
        {
            var polygons = Rectangle(0, 0, 100, 100);
            polygons.Add(new[] { new Vector2(25, 25), new Vector2(25, 75), new Vector2(75, 75), new Vector2(75, 25) });
            var mask = ShapeMask.Rasterize(polygons, 4, 100);
            Assert.That(new[] { mask[0], mask[5], mask[6], mask[15] }, Is.EqualTo(new[] { 1f, 0f, 0f, 1f }).Within(1e-4f));
        }

        private static List<Vector2[]> Rectangle(float left, float top, float right, float bottom)
        {
            return new List<Vector2[]> { new[] { new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom) } };
        }

        private static float[] Row(float[] mask, int size, int row)
        {
            return mask.Skip(row * size).Take(size).ToArray();
        }
    }
}