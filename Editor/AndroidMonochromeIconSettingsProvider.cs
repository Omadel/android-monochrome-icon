using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Project Settings page of the monochrome layer: its settings, and the checks of its image, run again whenever the
    ///     image, its file or its import settings change.
    /// </summary>
    internal class AndroidMonochromeIconSettingsProvider : SettingsProvider
    {
        private const string PATH = "Project/Android Monochrome Icon";
        private const double ADAPTIVE_ICON_CHECK_INTERVAL = 1;

        private static readonly GUIContent _enabledLabel = new GUIContent("Add Monochrome Layer", "Adds the image as the monochrome layer of the adaptive icon in Android builds. Android 13 and later tint it to the theme colours when themed icons are on; earlier versions ignore it.");
        private static readonly GUIContent _imageLabel = new GUIContent("Image", "PNG file copied as it is into the build. Android only keeps its opacity: draw the layer in white on a transparent background.");
        private static readonly GUIContent _failBuildLabel = new GUIContent("Fail Build On Error", "Fails the build when the image has an error. Otherwise the error is logged as a warning and the icon is built without the monochrome layer. Warnings never fail the build.");
        private static readonly GUIContent _applyImportLabel = new GUIContent("Apply Import Settings", "Imports the image as Single Channel, from its Alpha channel.");

        private SerializedObject _settings;
        private GUIStyle _pageStyle;
        private List<ImageIssue> _imageIssues = new List<ImageIssue>();
        private string _checkedImageState;
        private bool _isAdaptiveIconMissing;
        private double _nextAdaptiveIconCheck;

        private AndroidMonochromeIconSettingsProvider() : base(PATH, SettingsScope.Project, new[] { "android", "icon", "adaptive", "themed", "monochrome" })
        {
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new AndroidMonochromeIconSettingsProvider();
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = new SerializedObject(AndroidMonochromeIconSettings.instance);
            _checkedImageState = null;
            _nextAdaptiveIconCheck = 0;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public override void OnDeactivate()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override void OnGUI(string searchContext)
        {
            _pageStyle ??= new GUIStyle { margin = new RectOffset(10, 10, 10, 10) };
            _settings.Update();
            var enabled = _settings.FindProperty("_enabled");
            var image = _settings.FindProperty("_image");
            using (new EditorGUILayout.VerticalScope(_pageStyle))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enabled, _enabledLabel);
                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    EditorGUILayout.PropertyField(_settings.FindProperty("_failBuildOnError"), _failBuildLabel);
                    EditorGUILayout.PropertyField(image, _imageLabel);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    _settings.ApplyModifiedProperties();
                    AndroidMonochromeIconSettings.instance.Save();
                }

                if (enabled.boolValue) DrawIssues((Texture2D)image.objectReferenceValue);
            }
        }

        /// <summary>
        ///     Checks of the image and of the adaptive icon, each in a box, errors first; the import settings error comes with
        ///     a button that fixes it.
        /// </summary>
        private void DrawIssues(Texture2D image)
        {
            RefreshIssues(image);
            EditorGUILayout.Space();
            var issues = new List<ImageIssue>(_imageIssues);
            if (_isAdaptiveIconMissing) issues.Add(new ImageIssue(ImageIssueKind.MissingAdaptiveIcon, "The Android Player Settings have no adaptive icon: the build has no adaptive icon to add the monochrome layer to."));

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("The image is ready: Android builds add it as the monochrome layer of the adaptive icon.", MessageType.Info);
                return;
            }

            foreach (var issue in issues)
            {
                EditorGUILayout.HelpBox(issue.Message, issue.IsError ? MessageType.Error : MessageType.Warning);
                if (issue.Kind != ImageIssueKind.NotSingleChannelAlpha) continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var path = AssetDatabase.GetAssetPath(image);
                    if (GUILayout.Button(_applyImportLabel, GUILayout.ExpandWidth(false))) EditorApplication.delayCall += () => ImageImportSettings.ApplySingleChannelAlpha(path);
                }
            }
        }

        /// <summary>
        ///     Runs the image checks again when the image, its file or its import settings changed since the last run, and the
        ///     adaptive icon check at most once per interval.
        /// </summary>
        private void RefreshIssues(Texture2D image)
        {
            var state = ImageState(image);
            if (state != _checkedImageState)
            {
                _imageIssues = ImageChecks.CheckImage(image);
                _checkedImageState = state;
            }

            if (EditorApplication.timeSinceStartup < _nextAdaptiveIconCheck) return;

            _isAdaptiveIconMissing = ImageChecks.IsAdaptiveIconMissing();
            _nextAdaptiveIconCheck = EditorApplication.timeSinceStartup + ADAPTIVE_ICON_CHECK_INTERVAL;
        }

        /// <summary>
        ///     What the image checks depend on: the image, the date of its file and its import settings.
        /// </summary>
        private static string ImageState(Texture2D image)
        {
            if (image == null) return string.Empty;

            var path = AssetDatabase.GetAssetPath(image);
            if (string.IsNullOrEmpty(path)) return image.GetInstanceID().ToString();

            var fullPath = Path.GetFullPath(path);
            var date = File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath).Ticks : 0;
            return $"{path}|{date}|{ImageImportSettings.IsSingleChannelAlpha(path)}";
        }

        private void OnUndoRedo()
        {
            AndroidMonochromeIconSettings.instance.Save();
            _settings.Update();
            Repaint();
        }
    }
}