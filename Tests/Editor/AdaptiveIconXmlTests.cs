using NUnit.Framework;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class AdaptiveIconXmlTests
    {
        private const string HEADER = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n    <background android:drawable=\"@mipmap/ic_launcher_background\"/>\n    <foreground android:drawable=\"@mipmap/ic_launcher_foreground\"/>\n";
        private const string ADAPTIVE_ICON = HEADER + "</adaptive-icon>";

        [Test]
        public void AddMonochrome_AdaptiveIcon_AddsMonochromeAsLastLayer()
        {
            var expected = HEADER + "    <monochrome android:drawable=\"@mipmap/app_icon_monochrome\"/>\n</adaptive-icon>";
            Assert.That(AdaptiveIconXml.AddMonochrome(ADAPTIVE_ICON, "app_icon_monochrome"), Is.EqualTo(expected));
        }

        [Test]
        public void AddMonochrome_AlreadyAdded_ReturnsXmlUnchanged()
        {
            var xml = AdaptiveIconXml.AddMonochrome(ADAPTIVE_ICON, "app_icon_monochrome");
            Assert.That(AdaptiveIconXml.AddMonochrome(xml, "app_icon_monochrome"), Is.EqualTo(xml));
        }

        [Test]
        public void AddMonochrome_OtherMonochrome_ReturnsXmlUnchanged()
        {
            var xml = HEADER + "    <monochrome android:drawable=\"@drawable/other\"/>\n</adaptive-icon>";
            Assert.That(AdaptiveIconXml.AddMonochrome(xml, "app_icon_monochrome"), Is.EqualTo(xml));
        }

        [Test]
        public void AddMonochrome_NoAdaptiveIcon_ReturnsXmlUnchanged()
        {
            const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<bitmap xmlns:android=\"http://schemas.android.com/apk/res/android\" android:src=\"@mipmap/app_icon\"/>";
            Assert.That(AdaptiveIconXml.AddMonochrome(xml, "app_icon_monochrome"), Is.EqualTo(xml));
        }
    }
}