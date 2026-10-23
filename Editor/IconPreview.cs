using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Pixels of a themed icon preview, drawn as Android does: the monochrome layer is the 108 dp foreground of an adaptive
    ///     icon whose mask shows its central 72 dp, over a background of one colour, and only its opacity counts.
    /// </summary>
    internal static class IconPreview
    {
        /// <summary>
        ///     Part of the layer side that the mask shows, 72 dp of 108 dp.
        /// </summary>
        internal const float VISIBLE_FRACTION = 2f / 3f;

        /// <summary>
        ///     Opacity of the visible centre of the layer image, from 0 to 1, averaged down to a size × size grid whose rows
        ///     go from the top. The pixels are those of a texture, whose rows go from the bottom.
        /// </summary>
        internal static float[] SampleVisibleLayer(Color32[] pixels, int width, int height, int size)
        {
            var alpha = new float[size * size];
            var inset = (1 - VISIBLE_FRACTION) / 2;
            for (var y = 0; y < size; y++)
            {
                var top = height * (inset + VISIBLE_FRACTION * y / size);
                var bottom = height * (inset + VISIBLE_FRACTION * (y + 1) / size);
                for (var x = 0; x < size; x++)
                {
                    var left = width * (inset + VISIBLE_FRACTION * x / size);
                    var right = width * (inset + VISIBLE_FRACTION * (x + 1) / size);
                    alpha[y * size + x] = AverageAlpha(pixels, width, height, left, right, top, bottom);
                }
            }

            return alpha;
        }

        /// <summary>
        ///     Pixels of the preview, rows from the bottom as a texture takes them: the background colour inside the mask,
        ///     and the layer over it in the foreground colour. The mask and the layer opacity go row by row from the top.
        /// </summary>
        internal static Color32[] Compose(float[] mask, float[] layer, int size, Color32 background, Color32 foreground)
        {
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var colour = Color32.Lerp(background, foreground, layer[index]);
                    colour.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(mask[index]) * 255);
                    pixels[(size - 1 - y) * size + x] = colour;
                }
            }

            return pixels;
        }

        /// <summary>
        ///     Mean opacity of the image over a rectangle in pixels, from the top, each pixel weighted by the part of it
        ///     inside the rectangle.
        /// </summary>
        private static float AverageAlpha(Color32[] pixels, int width, int height, float left, float right, float top, float bottom)
        {
            var sum = 0f;
            var area = 0f;
            for (var y = (int)top; y < Mathf.Min(height, Mathf.CeilToInt(bottom)); y++)
            {
                var weightY = Mathf.Min(y + 1, bottom) - Mathf.Max(y, top);
                for (var x = (int)left; x < Mathf.Min(width, Mathf.CeilToInt(right)); x++)
                {
                    var weight = weightY * (Mathf.Min(x + 1, right) - Mathf.Max(x, left));
                    sum += weight * pixels[(height - 1 - y) * width + x].a / 255f;
                    area += weight;
                }
            }

            return area > 0 ? sum / area : 0;
        }
    }
}