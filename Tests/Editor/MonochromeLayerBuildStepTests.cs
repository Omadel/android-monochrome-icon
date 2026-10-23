using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Build;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class MonochromeLayerBuildStepTests
    {
        private const string ADAPTIVE_ICON = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n    <background android:drawable=\"@mipmap/ic_launcher_background\"/>\n    <foreground android:drawable=\"@mipmap/ic_launcher_foreground\"/>\n</adaptive-icon>";

        private static readonly byte[] _image = { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };

        private string _resources;

        [SetUp]
        public void SetUp()
        {
            _resources = Path.Combine(Path.GetTempPath(), "AndroidMonochromeIconTests", Guid.NewGuid().ToString("N"), "res");
            Directory.CreateDirectory(Path.Combine(_resources, "mipmap-anydpi-v26"));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(Path.GetDirectoryName(_resources), true);
        }

        [Test]
        public void WriteLayer_BothAdaptiveIcons_CopiesImageAndAddsLayerToBoth()
        {
            WriteIcon("app_icon.xml");
            WriteIcon("app_icon_round.xml");
            Assert.That(MonochromeLayerBuildStep.WriteLayer(_resources, _image), Is.Empty);
            Assert.That(File.ReadAllBytes(ImagePath()), Is.EqualTo(_image));
            Assert.That(ReadIcon("app_icon.xml"), Does.Contain("<monochrome android:drawable=\"@mipmap/app_icon_monochrome\"/>"));
            Assert.That(ReadIcon("app_icon_round.xml"), Does.Contain("<monochrome android:drawable=\"@mipmap/app_icon_monochrome\"/>"));
        }

        [Test]
        public void WriteLayer_RunTwice_AddsLayerOnce()
        {
            WriteIcon("app_icon.xml");
            MonochromeLayerBuildStep.WriteLayer(_resources, _image);
            MonochromeLayerBuildStep.WriteLayer(_resources, _image);
            Assert.That(ReadIcon("app_icon.xml").Split(new[] { "<monochrome" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
        }

        [Test]
        public void WriteLayer_OneAdaptiveIcon_AddsLayerWithoutIssue()
        {
            WriteIcon("app_icon.xml");
            Assert.That(MonochromeLayerBuildStep.WriteLayer(_resources, _image), Is.Empty);
            Assert.That(ReadIcon("app_icon.xml"), Does.Contain("<monochrome"));
            Assert.That(File.Exists(Path.Combine(_resources, "mipmap-anydpi-v26", "app_icon_round.xml")), Is.False);
        }

        [Test]
        public void WriteLayer_NoAdaptiveIcon_WritesNothingAndReturnsWarning()
        {
            var issues = MonochromeLayerBuildStep.WriteLayer(_resources, _image);
            Assert.That(issues.Select(issue => issue.Kind), Is.EqualTo(new[] { ImageIssueKind.MissingAdaptiveIcon }));
            Assert.That(issues[0].IsError, Is.False);
            Assert.That(File.Exists(ImagePath()), Is.False);
        }

        [Test]
        public void Report_NoIssue_ReturnsTrueWithoutLog()
        {
            var logs = new List<object>();
            Assert.That(MonochromeLayerBuildStep.Report(new List<ImageIssue>(), true, logs.Add), Is.True);
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void Report_ErrorWithFailBuild_ThrowsBuildFailedException()
        {
            var issues = new List<ImageIssue> { new ImageIssue(ImageIssueKind.NotWhite, "Not white.") };
            Assert.That(() => MonochromeLayerBuildStep.Report(issues, true, new List<object>().Add), Throws.TypeOf<BuildFailedException>().With.Message.Contains("Not white."));
        }

        [Test]
        public void Report_ErrorWithoutFailBuild_LogsWarningAndReturnsFalse()
        {
            var logs = new List<object>();
            var issues = new List<ImageIssue> { new ImageIssue(ImageIssueKind.NotWhite, "Not white.") };
            Assert.That(MonochromeLayerBuildStep.Report(issues, false, logs.Add), Is.False);
            Assert.That(logs, Is.EqualTo(new[] { "Android Monochrome Icon: Not white. The icon is built without the monochrome layer." }));
        }

        [Test]
        public void Report_WarningWithFailBuild_LogsWarningAndReturnsTrue()
        {
            var logs = new List<object>();
            var issues = new List<ImageIssue> { new ImageIssue(ImageIssueKind.UnexpectedSize, "Not 432 px.") };
            Assert.That(MonochromeLayerBuildStep.Report(issues, true, logs.Add), Is.True);
            Assert.That(logs, Is.EqualTo(new[] { "Android Monochrome Icon: Not 432 px." }));
        }

        private void WriteIcon(string name)
        {
            File.WriteAllText(Path.Combine(_resources, "mipmap-anydpi-v26", name), ADAPTIVE_ICON);
        }

        private string ReadIcon(string name)
        {
            return File.ReadAllText(Path.Combine(_resources, "mipmap-anydpi-v26", name));
        }

        private string ImagePath()
        {
            return Path.Combine(_resources, "mipmap-xxxhdpi", MonochromeLayerBuildStep.RESOURCE_NAME + ".png");
        }
    }
}