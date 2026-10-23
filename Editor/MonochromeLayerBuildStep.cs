using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Adds the monochrome layer to the adaptive icon of the generated Gradle project, which Unity 6 does not write:
    ///     Android 13 and later tint it to the theme colours when themed icons are on, earlier versions ignore it. The image
    ///     goes to the xxxhdpi density only, Android scales it down for the others.
    /// </summary>
    internal class MonochromeLayerBuildStep : IPostGenerateGradleAndroidProject
    {
        internal const string RESOURCE_NAME = "app_icon_monochrome";
        private const string LOG_PREFIX = "Android Monochrome Icon: ";

        private static readonly string[] _adaptiveIcons = { "app_icon.xml", "app_icon_round.xml" };

        public int callbackOrder => 0;

        /// <summary>
        ///     Checks the image of the settings again, then writes the layer into the resources of the launcher module, next
        ///     to the Unity library module whose folder Unity passes.
        /// </summary>
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var settings = AndroidMonochromeIconSettings.instance;
            if (!settings.Enabled) return;

            if (!Report(ImageChecks.CheckImage(settings.Image), settings.FailBuildOnError, Debug.LogWarning)) return;

            var image = File.ReadAllBytes(Path.GetFullPath(AssetDatabase.GetAssetPath(settings.Image)));
            Report(WriteLayer(Path.Combine(path, "..", "launcher", "src", "main", "res"), image), settings.FailBuildOnError, Debug.LogWarning);
        }

        /// <summary>
        ///     Writes the layer into a res folder: the image as it is in mipmap-xxxhdpi, and a monochrome layer in each
        ///     adaptive icon XML. Without any adaptive icon XML, writes nothing and returns a warning.
        /// </summary>
        internal static List<ImageIssue> WriteLayer(string resources, byte[] image)
        {
            var icons = _adaptiveIcons.Select(icon => Path.Combine(resources, "mipmap-anydpi-v26", icon)).Where(File.Exists).ToList();
            if (icons.Count == 0) return new List<ImageIssue> { new ImageIssue(ImageIssueKind.MissingAdaptiveIcon, "The Gradle project has no adaptive icon to add the monochrome layer to: set adaptive icons in the Android Player Settings.") };

            var mipmaps = Path.Combine(resources, "mipmap-xxxhdpi");
            Directory.CreateDirectory(mipmaps);
            File.WriteAllBytes(Path.Combine(mipmaps, RESOURCE_NAME + ".png"), image);
            foreach (var icon in icons)
            {
                File.WriteAllText(icon, AdaptiveIconXml.AddMonochrome(File.ReadAllText(icon), RESOURCE_NAME));
            }

            return new List<ImageIssue>();
        }

        /// <summary>
        ///     Logs each issue as a warning, or fails the build on an error if the settings ask for it. Returns whether there
        ///     was no error, which lets the build write the layer.
        /// </summary>
        internal static bool Report(List<ImageIssue> issues, bool failBuildOnError, Action<object> logWarning)
        {
            var errors = issues.Where(issue => issue.IsError).ToList();
            if (errors.Count > 0 && failBuildOnError) throw new BuildFailedException(LOG_PREFIX + string.Join(" ", errors.Select(error => error.Message)));

            foreach (var issue in issues)
            {
                logWarning(LOG_PREFIX + issue.Message + (issue.IsError ? " The icon is built without the monochrome layer." : string.Empty));
            }

            return errors.Count == 0;
        }
    }
}