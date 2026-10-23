namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Edits the adaptive icon XML files of the generated Gradle project, keeping the formatting Unity writes.
    /// </summary>
    internal static class AdaptiveIconXml
    {
        /// <summary>
        ///     Adaptive icon XML with a monochrome layer pointing to a mipmap, added as its last layer unless it already has
        ///     one. An XML without an adaptive icon is returned as is.
        /// </summary>
        internal static string AddMonochrome(string xml, string resourceName)
        {
            if (xml.Contains("<monochrome")) return xml;

            return xml.Replace("</adaptive-icon>", $"    <monochrome android:drawable=\"@mipmap/{resourceName}\"/>\n</adaptive-icon>");
        }
    }
}