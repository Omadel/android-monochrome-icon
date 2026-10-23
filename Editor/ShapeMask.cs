using System.Collections.Generic;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Fills polygons into a square coverage mask, with the non-zero winding rule of Android paths. Each pixel row is
    ///     sampled on several lines, and each line covers its pixels exactly along x, which smooths the edges.
    /// </summary>
    internal static class ShapeMask
    {
        private const int ROW_SAMPLES = 8;

        /// <summary>
        ///     Coverage of each pixel, from 0 to 1, of a size × size mask whose rows go from the top, for polygons drawn in a
        ///     square viewport of the given side.
        /// </summary>
        internal static float[] Rasterize(List<Vector2[]> polygons, int size, float viewport)
        {
            var coverage = new float[size * size];
            var scale = size / viewport;
            var crossings = new List<(float x, int winding)>();
            for (var line = 0; line < size * ROW_SAMPLES; line++)
            {
                var y = (line + 0.5f) / ROW_SAMPLES;
                crossings.Clear();
                foreach (var polygon in polygons)
                {
                    for (var i = 0; i < polygon.Length; i++)
                    {
                        var from = polygon[i] * scale;
                        var to = polygon[(i + 1) % polygon.Length] * scale;
                        if (from.y == to.y || y < Mathf.Min(from.y, to.y) || y >= Mathf.Max(from.y, to.y)) continue;

                        crossings.Add((from.x + (y - from.y) / (to.y - from.y) * (to.x - from.x), from.y < to.y ? 1 : -1));
                    }
                }

                crossings.Sort((first, second) => first.x.CompareTo(second.x));
                var winding = 0;
                for (var i = 0; i < crossings.Count - 1; i++)
                {
                    winding += crossings[i].winding;
                    if (winding != 0) AddSpan(coverage, size, line / ROW_SAMPLES, crossings[i].x, crossings[i + 1].x);
                }
            }

            for (var i = 0; i < coverage.Length; i++)
            {
                coverage[i] = Mathf.Min(1, coverage[i]);
            }

            return coverage;
        }

        /// <summary>
        ///     Adds a filled span of one sampling line to the pixels of its row, in proportion to the part of each pixel it
        ///     covers.
        /// </summary>
        private static void AddSpan(float[] coverage, int size, int row, float start, float end)
        {
            var left = Mathf.Clamp(start, 0, size);
            var right = Mathf.Clamp(end, 0, size);
            if (right <= left) return;

            var weight = 1f / ROW_SAMPLES;
            var first = (int)left;
            var last = Mathf.Min((int)right, size - 1);
            if (first == last)
            {
                coverage[row * size + first] += (right - left) * weight;
                return;
            }

            coverage[row * size + first] += (first + 1 - left) * weight;
            for (var x = first + 1; x < last; x++)
            {
                coverage[row * size + x] += weight;
            }

            coverage[row * size + last] += (right - last) * weight;
        }
    }
}