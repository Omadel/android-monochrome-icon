using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class PathDataTests
    {
        [Test]
        public void Flatten_AbsoluteLines_ReturnsTheirPoints()
        {
            AssertPoints(PathData.Flatten("M0,0L100,0 100,100 0,100z"), new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(0, 100));
        }

        [Test]
        public void Flatten_RelativeHorizontalAndVerticalLines_MoveFromCurrentPoint()
        {
            AssertPoints(PathData.Flatten("m10 10h20v20h-20z"), new Vector2(10, 10), new Vector2(30, 10), new Vector2(30, 30), new Vector2(10, 30));
        }

        [Test]
        public void Flatten_RelativeMoveWithMorePairs_DrawsRelativeLines()
        {
            AssertPoints(PathData.Flatten("m10 10 20 0 0 20z"), new Vector2(10, 10), new Vector2(30, 10), new Vector2(30, 30));
        }

        [Test]
        public void Flatten_NumbersWithoutSeparators_SplitsThem()
        {
            AssertPoints(PathData.Flatten("M.5-1.5L1.5.5 3,3z"), new Vector2(0.5f, -1.5f), new Vector2(1.5f, 0.5f), new Vector2(3, 3));
        }

        [Test]
        public void Flatten_TwoSubpaths_ReturnsTwoPolygons()
        {
            Assert.That(PathData.Flatten("M0 0H10V10Z M20 20H30V30Z").Count, Is.EqualTo(2));
        }

        [Test]
        public void Flatten_CubicCurve_PassesThroughItsMiddleAndEnd()
        {
            var points = PathData.Flatten("M0 0C0 100 100 100 100 0Z")[0];
            Assert.That(points.Any(point => Vector2.Distance(point, new Vector2(50, 75)) < 0.01f), Is.True);
            Assert.That(Vector2.Distance(points[points.Length - 1], new Vector2(100, 0)), Is.LessThan(0.01f));
        }

        [Test]
        public void Flatten_SmoothCubic_MirrorsLastControlPoint()
        {
            AssertSamePolygons(PathData.Flatten("M0 0C0 10 10 10 10 0S20 -10 20 0"), PathData.Flatten("M0 0C0 10 10 10 10 0C10 -10 20 -10 20 0"));
        }

        [Test]
        public void Flatten_SmoothQuadratic_MirrorsLastControlPoint()
        {
            AssertSamePolygons(PathData.Flatten("M0 0Q5 10 10 0T20 0"), PathData.Flatten("M0 0Q5 10 10 0Q15 -10 20 0"));
        }

        [Test]
        public void Flatten_TwoHalfCircleArcs_EncloseCircleArea()
        {
            var polygons = PathData.Flatten("M50 0A50 50,0,1,1,50 100A50 50,0,1,1,50 0");
            Assert.That(Area(polygons[0]), Is.EqualTo(Mathf.PI * 50 * 50).Within(0.5).Percent);
            Assert.That(polygons[0].Select(point => Vector2.Distance(point, new Vector2(50, 50))), Is.All.EqualTo(50).Within(0.01));
        }

        [Test]
        public void Flatten_EveryIconShape_GivesOnePolygonInTheViewport()
        {
            foreach (var shape in IconShape.All)
            {
                var polygons = PathData.Flatten(shape.Path);
                Assert.That(polygons.Count, Is.EqualTo(1), shape.Name);
                Assert.That(polygons[0].Select(point => Mathf.Min(point.x, point.y)), Is.All.GreaterThan(-2f), shape.Name);
                Assert.That(polygons[0].Select(point => Mathf.Max(point.x, point.y)), Is.All.LessThan(102f), shape.Name);
                Assert.That(Area(polygons[0]), Is.InRange(5000f, 10001f), shape.Name);
            }
        }

        private static void AssertPoints(List<Vector2[]> polygons, params Vector2[] expected)
        {
            Assert.That(polygons.Count, Is.EqualTo(1));
            Assert.That(polygons[0].Length, Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(Vector2.Distance(polygons[0][i], expected[i]), Is.LessThan(1e-4f), $"point {i}: {polygons[0][i]}");
            }
        }

        private static void AssertSamePolygons(List<Vector2[]> actual, List<Vector2[]> expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.That(actual[i].Length, Is.EqualTo(expected[i].Length));
                for (var j = 0; j < expected[i].Length; j++)
                {
                    Assert.That(Vector2.Distance(actual[i][j], expected[i][j]), Is.LessThan(1e-4f));
                }
            }
        }

        /// <summary>
        ///     Area of a polygon by the shoelace formula, whatever its direction.
        /// </summary>
        private static float Area(Vector2[] polygon)
        {
            var sum = 0f;
            for (var i = 0; i < polygon.Length; i++)
            {
                var next = polygon[(i + 1) % polygon.Length];
                sum += polygon[i].x * next.y - next.x * polygon[i].y;
            }

            return Mathf.Abs(sum) / 2;
        }
    }
}