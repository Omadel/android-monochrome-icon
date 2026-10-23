using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Mask shape of adaptive icons, read from a PNG file named after it: its opaque part is the inside of the mask,
    ///     stretched over the 72 dp the mask shows. The package brings the 14 shapes found in AOSP, in its Shapes folder; a
    ///     project can add a folder of its own.
    /// </summary>
    internal readonly struct IconShape
    {
        private const string PACKAGE_FOLDER = "Shapes";
        private const string EXTENSION = ".png";

        internal IconShape(string name, string filePath)
        {
            Name = name;
            FilePath = filePath;
        }

        internal string Name { get; }

        /// <summary>
        ///     Path of the PNG file on disk.
        /// </summary>
        internal string FilePath { get; }

        /// <summary>
        ///     Folder of the shapes the package brings, on disk.
        /// </summary>
        internal static string PackageFolder => Path.Combine(PackageInfo.FindForAssembly(typeof(IconShape).Assembly).resolvedPath, PACKAGE_FOLDER);

        /// <summary>
        ///     Shapes of the PNG files in a folder on disk and its subfolders, in the order of their names; none when the
        ///     folder does not exist.
        /// </summary>
        internal static List<IconShape> Find(string folder)
        {
            if (!Directory.Exists(folder)) return new List<IconShape>();

            return Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Where(file => string.Equals(Path.GetExtension(file), EXTENSION, StringComparison.OrdinalIgnoreCase)).Select(file => new IconShape(Path.GetFileNameWithoutExtension(file), file)).OrderBy(shape => shape.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        ///     Coverage of a shape file on a size × size grid, from 0 to 1, rows from the top; null when the file is not a
        ///     readable PNG.
        /// </summary>
        internal static float[] Mask(byte[] file, int size)
        {
            if (file == null || !ImageChecks.IsPng(file) || !ImageChecks.TryReadPixels(file, out var width, out var height, out var pixels)) return null;

            return IconPreview.SampleAlpha(pixels, width, height, size, 0);
        }

        /// <summary>
        ///     Coverage of the shape on a size × size grid, read from its file; null when the file is gone or not a readable
        ///     PNG.
        /// </summary>
        internal float[] LoadMask(int size)
        {
            return File.Exists(FilePath) ? Mask(File.ReadAllBytes(FilePath), size) : null;
        }
    }
}