using UnityEditor;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Project settings of the monochrome layer, saved in the ProjectSettings folder: versioned with the project,
    ///     without an asset in Assets.
    /// </summary>
    [FilePath("ProjectSettings/AndroidMonochromeIconSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class AndroidMonochromeIconSettings : ScriptableSingleton<AndroidMonochromeIconSettings>
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private Texture2D _image;
        [SerializeField] private bool _failBuildOnError;

        public bool Enabled => _enabled;
        public Texture2D Image => _image;
        public bool FailBuildOnError => _failBuildOnError;

        /// <summary>
        ///     Lets the settings page edit the singleton, which Unity marks as not editable.
        /// </summary>
        private void OnEnable()
        {
            hideFlags &= ~HideFlags.NotEditable;
        }

        /// <summary>
        ///     Writes the settings to their file in the ProjectSettings folder, as text.
        /// </summary>
        internal void Save()
        {
            Save(true);
        }
    }
}