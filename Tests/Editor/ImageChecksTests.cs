using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class ImageChecksTests
    {
        private static readonly Color32 _white = new Color32(255, 255, 255, 255);
        private static readonly Color32 _clear = new Color32(255, 255, 255, 0);
        private static readonly Color32 _black = new Color32(0, 0, 0, 255);

        [Test]
        public void Check_NoFile_ReturnsMissingImage()
        {
            Assert.That(Kinds(ImageChecks.Check(null, true)), Is.EqualTo(new[] { ImageIssueKind.MissingImage }));
        }

        [Test]
        public void Check_WhiteSquareAtLayerSize_ReturnsNoIssue()
        {
            Assert.That(ImageChecks.Check(Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _white), true), Is.Empty);
        }

        [Test]
        public void Check_NotSingleChannelAlpha_ReturnsError()
        {
            Assert.That(Kinds(ImageChecks.Check(Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _white), false)), Is.EqualTo(new[] { ImageIssueKind.NotSingleChannelAlpha }));
        }

        [Test]
        public void Check_JpgFile_ReturnsNotPng()
        {
            var texture = Texture(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _white);
            var jpg = texture.EncodeToJPG();
            Object.DestroyImmediate(texture);
            Assert.That(Kinds(ImageChecks.Check(jpg, true)), Is.EqualTo(new[] { ImageIssueKind.NotPng }));
        }

        [Test]
        public void Check_PngSignatureWithoutImage_ReturnsNotPng()
        {
            var file = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
            Assert.That(Kinds(ImageChecks.Check(file, true)), Is.EqualTo(new[] { ImageIssueKind.NotPng }));
        }

        [Test]
        public void Check_NotSquare_ReturnsNotSquareWithoutSizeWarning()
        {
            Assert.That(Kinds(ImageChecks.Check(Png(ImageChecks.LAYER_SIZE, 400, _white), true)), Is.EqualTo(new[] { ImageIssueKind.NotSquare }));
        }

        [Test]
        public void Check_VisibleColouredPixel_ReturnsNotGrey()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _clear, new Color32(255, 0, 0, 255));
            Assert.That(Kinds(ImageChecks.Check(file, true)), Is.EqualTo(new[] { ImageIssueKind.NotGrey }));
        }

        [Test]
        public void Check_FaintOffGreyPixel_ReturnsNotGrey()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _clear, new Color32(254, 255, 255, 1));
            Assert.That(Kinds(ImageChecks.Check(file, true)), Is.EqualTo(new[] { ImageIssueKind.NotGrey }));
        }

        [Test]
        public void Check_TransparentColouredPixel_ReturnsNoIssue()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _white, new Color32(255, 0, 0, 0));
            Assert.That(ImageChecks.Check(file, true), Is.Empty);
        }

        [Test]
        public void Check_GreyPixel_ReturnsNoIssue()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _clear, new Color32(128, 128, 128, 255));
            Assert.That(ImageChecks.Check(file, true), Is.Empty);
        }

        [Test]
        public void Check_WhiteOnOpaqueBlack_ReturnsNoIssue()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _black, _white);
            Assert.That(ImageChecks.Check(file, true), Is.Empty);
        }

        [Test]
        public void Check_BlackOnTransparent_ReturnsEmptyLayer()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _clear, _black);
            Assert.That(Kinds(ImageChecks.Check(file, true)), Is.EqualTo(new[] { ImageIssueKind.EmptyLayer }));
        }

        [Test]
        public void Check_FullyTransparent_ReturnsEmptyLayer()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _clear);
            Assert.That(Kinds(ImageChecks.Check(file, true)), Is.EqualTo(new[] { ImageIssueKind.EmptyLayer }));
        }

        [Test]
        public void Check_PartlyTransparentWhitePixel_ReturnsNoIssue()
        {
            var file = Png(ImageChecks.LAYER_SIZE, ImageChecks.LAYER_SIZE, _clear, new Color32(255, 255, 255, 128));
            Assert.That(ImageChecks.Check(file, true), Is.Empty);
        }

        [Test]
        public void Check_OtherSquareSize_ReturnsUnexpectedSize()
        {
            Assert.That(Kinds(ImageChecks.Check(Png(256, 256, _white), true)), Is.EqualTo(new[] { ImageIssueKind.UnexpectedSize }));
        }

        [Test]
        public void Check_SeveralProblems_ReturnsErrorsBeforeWarnings()
        {
            var file = Png(256, 256, _clear, new Color32(255, 0, 0, 255));
            Assert.That(Kinds(ImageChecks.Check(file, false)), Is.EqualTo(new[] { ImageIssueKind.NotSingleChannelAlpha, ImageIssueKind.NotGrey, ImageIssueKind.UnexpectedSize }));
        }

        [Test]
        public void IsError_ErrorKinds_ReturnsTrue()
        {
            var kinds = new[] { ImageIssueKind.MissingImage, ImageIssueKind.NotSingleChannelAlpha, ImageIssueKind.NotPng, ImageIssueKind.NotSquare, ImageIssueKind.NotGrey, ImageIssueKind.EmptyLayer };
            Assert.That(kinds.Select(kind => new ImageIssue(kind, string.Empty).IsError), Is.All.True);
        }

        [Test]
        public void IsError_WarningKinds_ReturnsFalse()
        {
            var kinds = new[] { ImageIssueKind.UnexpectedSize, ImageIssueKind.MissingAdaptiveIcon };
            Assert.That(kinds.Select(kind => new ImageIssue(kind, string.Empty).IsError), Is.All.False);
        }

        [Test]
        public void IsPng_TooShort_ReturnsFalse()
        {
            Assert.That(ImageChecks.IsPng(new byte[] { 0x89, 0x50, 0x4E }), Is.False);
        }

        [Test]
        public void CountColouredPixels_MixedPixels_CountsVisibleColouredOnes()
        {
            var pixels = new[] { _white, _clear, _black, new Color32(128, 128, 128, 1), new Color32(255, 0, 0, 0), new Color32(255, 255, 0, 255), new Color32(1, 0, 0, 1) };
            Assert.That(ImageChecks.CountColouredPixels(pixels), Is.EqualTo(2));
        }

        private static ImageIssueKind[] Kinds(IEnumerable<ImageIssue> issues)
        {
            return issues.Select(issue => issue.Kind).ToArray();
        }

        /// <summary>
        ///     PNG file filled with one colour, with the first pixel of another colour if given.
        /// </summary>
        private static byte[] Png(int width, int height, Color32 fill, Color32? firstPixel = null)
        {
            var texture = Texture(width, height, fill, firstPixel);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        private static Texture2D Texture(int width, int height, Color32 fill, Color32? firstPixel = null)
        {
            var pixels = Enumerable.Repeat(fill, width * height).ToArray();
            if (firstPixel.HasValue) pixels[0] = firstPixel.Value;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}