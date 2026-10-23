using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Project Settings page of the monochrome layer: its settings, the checks of its image, run again whenever the
    ///     image, its file or its import settings change, and previews of the themed icon for each mask shape, those of the
    ///     package and those of the project.
    /// </summary>
    internal class AndroidMonochromeIconSettingsProvider : SettingsProvider
    {
        private const string PATH = "Project/Android Monochrome Icon";
        private const string BASE_COLOUR_KEY = "Javienne.AndroidMonochromeIcon.PreviewBaseColour";
        private const double ADAPTIVE_ICON_CHECK_INTERVAL = 1;
        private const double SHAPES_CHECK_INTERVAL = 1;
        private const float PREVIEW_SIZE = 48;
        private const float PREVIEW_SPACING = 8;
        private const float PREVIEW_PADDING = 8;

        private static readonly GUIContent _enabledLabel = new GUIContent("Add Monochrome Layer", "Adds the image as the monochrome layer of the adaptive icon in Android builds. Android 13 and later tint it to the theme colours when themed icons are on; earlier versions ignore it.");
        private static readonly GUIContent _imageLabel = new GUIContent("Image", "PNG file copied as it is into the build. Android only keeps its opacity: draw the layer in white on a transparent background.");
        private static readonly GUIContent _failBuildLabel = new GUIContent("Fail Build On Error", "Fails the build when the image has an error. Otherwise the error is logged as a warning and the icon is built without the monochrome layer. Warnings never fail the build.");
        private static readonly GUIContent _applyImportLabel = new GUIContent("Apply Import Settings", "Imports the image as Single Channel, from its Alpha channel.");
        private static readonly GUIContent _previewLabel = new GUIContent("Themed Icon Preview", "The image as Android 14 and later draw it when themed icons are on: tinted to colours derived from the base colour, in each mask shape of Android.");
        private static readonly GUIContent _baseColourLabel = new GUIContent("Base Colour", "Colour Android derives the themed icon colours from: taken from the wallpaper, or picked in the wallpaper and style settings. Saved for you only, in the editor preferences.");
        private static readonly GUIContent _resetColourLabel = new GUIContent("Reset", "Back to the default blue of Android.");
        private static readonly GUIContent _shapesFolderLabel = new GUIContent("Extra Shapes", "Folder of PNG files, each a mask shape added to the previews after those of the package: its opaque part is the inside of the mask, stretched over the 72 dp the mask shows, and its file name names it. Subfolders count too.");
        private static readonly GUIContent _lightThemeLabel = new GUIContent("Light Theme");
        private static readonly GUIContent _darkThemeLabel = new GUIContent("Dark Theme");
        private static readonly Color _lightPanelColour = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color _darkPanelColour = new Color(0.1f, 0.1f, 0.1f);

        private SerializedObject _settings;
        private GUIStyle _pageStyle;
        private List<ImageIssue> _imageIssues = new List<ImageIssue>();
        private string _checkedImageState;
        private bool _isAdaptiveIconMissing;
        private double _nextAdaptiveIconCheck;
        private Color32 _baseColour;
        private List<IconShape> _shapes = new List<IconShape>();
        private string _shapesState;
        private double _nextShapesCheck;
        private List<string> _unreadableShapes = new List<string>();
        private Texture2D[] _previews;
        private GUIContent[] _previewTooltips;
        private string _previewState;
        private float _previewWidth = 400;

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
            _baseColour = LoadBaseColour();
            _nextShapesCheck = 0;
            _previewState = null;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public override void OnDeactivate()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyPreviews();
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

                if (!enabled.boolValue) return;

                DrawIssues((Texture2D)image.objectReferenceValue);
                DrawPreviews((Texture2D)image.objectReferenceValue);
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

        /// <summary>
        ///     Previews of the themed icon for each mask shape, in the light and dark themes, with the base colour of their
        ///     colours and the folder of the extra shapes. The previews need a readable PNG image.
        /// </summary>
        private void DrawPreviews(Texture2D image)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_previewLabel, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var colour = EditorGUILayout.ColorField(_baseColourLabel, _baseColour, true, false, false);
                if (EditorGUI.EndChangeCheck()) SetBaseColour(colour);

                if (GUILayout.Button(_resetColourLabel, GUILayout.ExpandWidth(false))) SetBaseColour(ThemeColours.DefaultBaseColour);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_settings.FindProperty("_shapesFolder"), _shapesFolderLabel);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.ApplyModifiedProperties();
                AndroidMonochromeIconSettings.instance.Save();
                _nextShapesCheck = 0;
            }

            if (AndroidMonochromeIconSettings.instance.ShapesFolder != null && ShapesFolderPath() == null) EditorGUILayout.HelpBox("Extra Shapes is not a folder: pick the folder that holds the PNG files of the shapes.", MessageType.Warning);

            if (!RefreshPreviews(image))
            {
                EditorGUILayout.LabelField("The previews appear once the image is a readable PNG file.", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            if (_unreadableShapes.Count > 0) EditorGUILayout.HelpBox($"Left out of the previews, not readable as PNG images: {string.Join(", ", _unreadableShapes)}.", MessageType.Warning);

            EditorGUILayout.LabelField("Colours are approximate: Android derives them from the base colour with its own colour model. Hover a preview to see its shape; device makers may use shapes of their own.", EditorStyles.wordWrappedMiniLabel);
            DrawPreviewPanel(_lightThemeLabel, 0, _lightPanelColour);
            DrawPreviewPanel(_darkThemeLabel, _previewTooltips.Length, _darkPanelColour);
        }

        /// <summary>
        ///     Previews of one theme on a panel, in as few rows as the width of the page allows, of even lengths; each
        ///     names its shape in its tooltip.
        /// </summary>
        private void DrawPreviewPanel(GUIContent label, int firstPreview, Color background)
        {
            var count = _previewTooltips.Length;
            if (count == 0) return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            var maxColumns = Mathf.Max(1, Mathf.FloorToInt((_previewWidth - 2 * PREVIEW_PADDING + PREVIEW_SPACING) / (PREVIEW_SIZE + PREVIEW_SPACING)));
            var rows = Mathf.CeilToInt((float)count / maxColumns);
            var columns = Mathf.CeilToInt((float)count / rows);
            var area = GUILayoutUtility.GetRect(0, 2 * PREVIEW_PADDING + rows * PREVIEW_SIZE + (rows - 1) * PREVIEW_SPACING, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint && area.width > 1 && !Mathf.Approximately(area.width, _previewWidth))
            {
                _previewWidth = area.width;
                Repaint();
            }

            var panel = new Rect(area.x, area.y, 2 * PREVIEW_PADDING + columns * PREVIEW_SIZE + (columns - 1) * PREVIEW_SPACING, area.height);
            EditorGUI.DrawRect(panel, background);
            for (var i = 0; i < count; i++)
            {
                var cell = new Rect(panel.x + PREVIEW_PADDING + i % columns * (PREVIEW_SIZE + PREVIEW_SPACING), panel.y + PREVIEW_PADDING + i / columns * (PREVIEW_SIZE + PREVIEW_SPACING), PREVIEW_SIZE, PREVIEW_SIZE);
                GUI.DrawTexture(cell, _previews[firstPreview + i]);
                GUI.Label(cell, _previewTooltips[i]);
            }
        }

        /// <summary>
        ///     Builds the preview textures again when the image, its file, the base colour, the shape files or the screen
        ///     density changed. Returns whether there are previews, which needs a readable PNG image.
        /// </summary>
        private bool RefreshPreviews(Texture2D image)
        {
            RefreshShapes();
            var size = Mathf.RoundToInt(PREVIEW_SIZE * EditorGUIUtility.pixelsPerPoint);
            var state = $"{ImageState(image)}|{ColorUtility.ToHtmlStringRGB(_baseColour)}|{size}|{_shapesState}";
            if (state == _previewState) return _previews != null;

            _previewState = state;
            DestroyPreviews();
            var file = image == null ? null : ImageChecks.ReadFile(image);
            if (file == null || !ImageChecks.IsPng(file) || !ImageChecks.TryReadPixels(file, out var width, out var height, out var pixels)) return false;

            var layer = IconPreview.SampleAlpha(pixels, width, height, size, IconPreview.LAYER_INSET);
            var masks = new List<float[]>();
            var tooltips = new List<GUIContent>();
            _unreadableShapes.Clear();
            foreach (var shape in _shapes)
            {
                var mask = shape.LoadMask(size);
                if (mask == null)
                {
                    _unreadableShapes.Add(shape.Name);
                    continue;
                }

                masks.Add(mask);
                tooltips.Add(new GUIContent(string.Empty, shape.Name));
            }

            _previewTooltips = tooltips.ToArray();
            _previews = new Texture2D[2 * masks.Count];
            for (var theme = 0; theme < 2; theme++)
            {
                var colours = ThemeColours.ForTheme(_baseColour, theme == 1);
                for (var i = 0; i < masks.Count; i++)
                {
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                    texture.SetPixels32(IconPreview.Compose(masks[i], layer, size, colours.background, colours.foreground));
                    texture.Apply();
                    _previews[theme * masks.Count + i] = texture;
                }
            }

            return true;
        }

        /// <summary>
        ///     Lists the shapes of the package and of the extra folder again, at most once per interval, with what the
        ///     previews depend on: the files and their dates.
        /// </summary>
        private void RefreshShapes()
        {
            if (EditorApplication.timeSinceStartup < _nextShapesCheck) return;

            _nextShapesCheck = EditorApplication.timeSinceStartup + SHAPES_CHECK_INTERVAL;
            var shapes = IconShape.Find(IconShape.PackageFolder);
            var folder = ShapesFolderPath();
            if (folder != null) shapes.AddRange(IconShape.Find(folder));

            _shapes = shapes;
            _shapesState = string.Join("|", shapes.Select(shape => $"{shape.FilePath}:{File.GetLastWriteTimeUtc(shape.FilePath).Ticks}"));
        }

        /// <summary>
        ///     Folder of the extra shapes on disk, or null when the settings have none or name an asset that is not a folder.
        /// </summary>
        private static string ShapesFolderPath()
        {
            var folder = AndroidMonochromeIconSettings.instance.ShapesFolder;
            var path = folder == null ? null : AssetDatabase.GetAssetPath(folder);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) ? FileUtil.GetPhysicalPath(path) : null;
        }

        private void DestroyPreviews()
        {
            if (_previews == null) return;

            foreach (var texture in _previews)
            {
                Object.DestroyImmediate(texture);
            }

            _previews = null;
        }

        /// <summary>
        ///     Base colour of the previews saved in the editor preferences, or the default blue of Android.
        /// </summary>
        private static Color32 LoadBaseColour()
        {
            return ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString(BASE_COLOUR_KEY), out var colour) ? (Color32)colour : ThemeColours.DefaultBaseColour;
        }

        private void SetBaseColour(Color32 colour)
        {
            _baseColour = new Color32(colour.r, colour.g, colour.b, 255);
            EditorPrefs.SetString(BASE_COLOUR_KEY, ColorUtility.ToHtmlStringRGB(_baseColour));
        }

        private void OnUndoRedo()
        {
            AndroidMonochromeIconSettings.instance.Save();
            _settings.Update();
            Repaint();
        }
    }
}