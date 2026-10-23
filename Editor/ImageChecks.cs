using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Checks of the monochrome image, run live by the settings page and again by the build. They read the PNG file
    ///     itself, which the build copies as it is, not the imported texture.
    /// </summary>
    internal static class ImageChecks
    {
        /// <summary>
        ///     Size of the 108 dp adaptive icon layer at the xxxhdpi density, the only one the build writes.
        /// </summary>
        internal const int LAYER_SIZE = 432;

        private static readonly byte[] _pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        ///     Problems of an image asset, errors first; a missing image is one.
        /// </summary>
        internal static List<ImageIssue> CheckImage(Texture2D image)
        {
            if (image == null) return Check(null, false);

            return Check(ReadFile(image), ImageImportSettings.IsSingleChannelAlpha(AssetDatabase.GetAssetPath(image)));
        }

        /// <summary>
        ///     Bytes of the file of an image asset, empty when the asset has no file.
        /// </summary>
        internal static byte[] ReadFile(Texture2D image)
        {
            var path = AssetDatabase.GetAssetPath(image);
            var fullPath = string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path);
            return fullPath != null && File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : new byte[0];
        }

        /// <summary>
        ///     Problems of an image file, errors first, from its bytes (null without image) and whether its texture is
        ///     imported as Single Channel Alpha.
        /// </summary>
        internal static List<ImageIssue> Check(byte[] file, bool isSingleChannelAlpha)
        {
            var issues = new List<ImageIssue>();
            if (file == null)
            {
                issues.Add(new ImageIssue(ImageIssueKind.MissingImage, "No image is set: the build leaves the monochrome layer out."));
                return issues;
            }

            if (!isSingleChannelAlpha) issues.Add(new ImageIssue(ImageIssueKind.NotSingleChannelAlpha, "Import the image as Single Channel, from its Alpha channel."));

            if (!IsPng(file) || !TryReadPixels(file, out var width, out var height, out var pixels))
            {
                issues.Add(new ImageIssue(ImageIssueKind.NotPng, "The image is not a PNG file: the build copies the file as it is, and Android needs a PNG."));
                return issues;
            }

            if (width != height) issues.Add(new ImageIssue(ImageIssueKind.NotSquare, $"The image is {width} × {height} px: the monochrome layer must be square."));

            var nonWhite = CountNonWhitePixels(pixels);
            if (nonWhite > 0) issues.Add(new ImageIssue(ImageIssueKind.NotWhite, $"{nonWhite} visible {(nonWhite == 1 ? "pixel is" : "pixels are")} not pure white: draw the layer in white (255, 255, 255), Android only keeps its opacity."));

            if (width == height && width != LAYER_SIZE) issues.Add(new ImageIssue(ImageIssueKind.UnexpectedSize, $"The image is {width} px wide: {LAYER_SIZE} px matches the 108 dp layer at the xxxhdpi density, which Android scales down for the others."));

            return issues;
        }

        /// <summary>
        ///     Whether the bytes start with the PNG signature.
        /// </summary>
        internal static bool IsPng(byte[] file)
        {
            if (file.Length < _pngSignature.Length) return false;

            for (var i = 0; i < _pngSignature.Length; i++)
            {
                if (file[i] != _pngSignature[i]) return false;
            }

            return true;
        }

        /// <summary>
        ///     Number of visible pixels, with any opacity above zero, that are not pure white.
        /// </summary>
        internal static int CountNonWhitePixels(Color32[] pixels)
        {
            var count = 0;
            foreach (var pixel in pixels)
            {
                if (pixel.a > 0 && (pixel.r != 255 || pixel.g != 255 || pixel.b != 255)) count++;
            }

            return count;
        }

        /// <summary>
        ///     Whether the Android Player Settings have no adaptive icon, which the monochrome layer is added to. False when
        ///     the editor cannot tell, without Android build support.
        /// </summary>
        internal static bool IsAdaptiveIconMissing()
        {
            var target = NamedBuildTarget.Android;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
            {
                var icons = PlayerSettings.GetPlatformIcons(target, kind);
                if (icons.Length == 0 || icons[0].maxLayerCount != 2) continue;

                foreach (var icon in icons)
                {
                    foreach (var texture in icon.GetTextures())
                    {
                        if (texture != null) return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Pixels of a PNG file, decoded as they are stored.
        /// </summary>
        internal static bool TryReadPixels(byte[] file, out int width, out int height, out Color32[] pixels)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                var loaded = texture.LoadImage(file);
                width = texture.width;
                height = texture.height;
                pixels = loaded ? texture.GetPixels32() : null;
                return loaded;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}