using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     The monochrome layer drawn from a greyscale image. Android only keeps the opacity of the layer, so the grey level
    ///     of each pixel becomes part of its opacity: white keeps all of it, black none.
    /// </summary>
    internal static class LayerPixels
    {
        /// <summary>
        ///     Opacity of a pixel in the layer, from 0 to 255: its own opacity times its grey level, the mean of its channels.
        /// </summary>
        internal static byte Opacity(Color32 pixel)
        {
            return (byte)((pixel.a * (pixel.r + pixel.g + pixel.b) + 382) / 765);
        }

        /// <summary>
        ///     Pixels of the layer, in the same order as those of the image: white, with the opacity of each pixel.
        /// </summary>
        internal static Color32[] FromImage(Color32[] pixels)
        {
            var layer = new Color32[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                layer[i] = new Color32(255, 255, 255, Opacity(pixels[i]));
            }

            return layer;
        }

        /// <summary>
        ///     Whether every visible pixel, with any opacity above zero, is pure white: the image is the layer already.
        /// </summary>
        internal static bool IsWhite(Color32[] pixels)
        {
            foreach (var pixel in pixels)
            {
                if (pixel.a > 0 && (pixel.r != 255 || pixel.g != 255 || pixel.b != 255)) return false;
            }

            return true;
        }

        /// <summary>
        ///     Whether the layer of the image has nothing to draw: every pixel is black or transparent.
        /// </summary>
        internal static bool IsEmpty(Color32[] pixels)
        {
            foreach (var pixel in pixels)
            {
                if (Opacity(pixel) > 0) return false;
            }

            return true;
        }

        /// <summary>
        ///     PNG file of the layer, from that of the image: the file as it is when the image is white, otherwise a new PNG
        ///     file of the layer pixels. Null when the file is not a readable PNG.
        /// </summary>
        internal static byte[] ToLayerFile(byte[] file)
        {
            if (!ImageChecks.IsPng(file) || !ImageChecks.TryReadPixels(file, out var width, out var height, out var pixels)) return null;

            if (IsWhite(pixels)) return file;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(FromImage(pixels));
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}