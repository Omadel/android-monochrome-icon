using UnityEditor;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Import settings the monochrome image needs: Single Channel, from its Alpha channel, as Android only keeps the
    ///     opacity of the layer. The build reads the file, so these settings only change the texture in the editor.
    /// </summary>
    internal static class ImageImportSettings
    {
        /// <summary>
        ///     Whether the texture at this asset path is imported as Single Channel from its Alpha channel.
        /// </summary>
        internal static bool IsSingleChannelAlpha(string assetPath)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer)) return false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.textureType == TextureImporterType.SingleChannel && settings.singleChannelComponent == TextureImporterSingleChannelComponent.Alpha;
        }

        /// <summary>
        ///     Imports the texture at this asset path as Single Channel from its Alpha channel.
        /// </summary>
        internal static void ApplySingleChannelAlpha(string assetPath)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer)) return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.ApplyTextureType(TextureImporterType.SingleChannel);
            settings.singleChannelComponent = TextureImporterSingleChannelComponent.Alpha;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}