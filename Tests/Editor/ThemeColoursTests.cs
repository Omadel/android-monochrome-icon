using System;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon.Tests
{
    public class ThemeColoursTests
    {
        private static readonly Color32[] _colours = { new Color32(0x1B, 0x6E, 0xF3, 255), new Color32(0xE5, 0x39, 0x35, 255), new Color32(0x43, 0xA0, 0x47, 255), new Color32(0xFD, 0xD8, 0x35, 255), new Color32(128, 128, 128, 255), new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 255) };

        [Test]
        public void ToLch_DefaultBlue_MatchesReference()
        {
            var lch = ThemeColours.ToLch(ThemeColours.DefaultBaseColour);
            Assert.That(lch.lightness, Is.EqualTo(49.3).Within(0.2));
            Assert.That(lch.chroma, Is.EqualTo(77.6).Within(0.2));
            Assert.That(lch.hue, Is.EqualTo(289.9).Within(0.2));
        }

        [Test]
        public void FromLch_ToLchOfColour_ReturnsSameColour()
        {
            foreach (var colour in _colours)
            {
                var lch = ThemeColours.ToLch(colour);
                var result = ThemeColours.FromLch(lch.lightness, lch.chroma, lch.hue);
                Assert.That(new[] { result.r, result.g, result.b }, Is.EqualTo(new[] { colour.r, colour.g, colour.b }).Within(1), colour.ToString());
            }
        }

        [Test]
        public void FromLch_OutOfGamut_LowersChromaOnly()
        {
            var lch = ThemeColours.ToLch(ThemeColours.FromLch(90, 120, 90));
            Assert.That(lch.lightness, Is.EqualTo(90).Within(0.5));
            Assert.That(lch.hue, Is.EqualTo(90).Within(2));
            Assert.That(lch.chroma, Is.LessThan(120));
        }

        [Test]
        public void ForTheme_DefaultBlue_StaysCloseToAndroid()
        {
            var light = ThemeColours.ForTheme(ThemeColours.DefaultBaseColour, false);
            var dark = ThemeColours.ForTheme(ThemeColours.DefaultBaseColour, true);
            Assert.That(DeltaE(light.background, new Color32(0xD9, 0xE2, 0xFF, 255)), Is.LessThan(5));
            Assert.That(DeltaE(light.foreground, new Color32(0x33, 0x46, 0x71, 255)), Is.LessThan(5));
            Assert.That(DeltaE(dark.background, new Color32(0x2A, 0x30, 0x42, 255)), Is.LessThan(5));
            Assert.That(DeltaE(dark.foreground, new Color32(0xB8, 0xC6, 0xEE, 255)), Is.LessThan(5));
        }

        [Test]
        public void ForTheme_AnyBase_KeepsRoleTones()
        {
            foreach (var colour in _colours)
            {
                var light = ThemeColours.ForTheme(colour, false);
                var dark = ThemeColours.ForTheme(colour, true);
                var tones = new[] { ThemeColours.ToLch(light.background).lightness, ThemeColours.ToLch(light.foreground).lightness, ThemeColours.ToLch(dark.background).lightness, ThemeColours.ToLch(dark.foreground).lightness };
                Assert.That(tones, Is.EqualTo(new[] { 90.0, 30.0, 20.0, 80.0 }).Within(0.5), colour.ToString());
            }
        }

        [Test]
        public void ForTheme_GreyBase_UsesDefaultBlue()
        {
            var grey = new Color32(128, 128, 128, 255);
            Assert.That(ThemeColours.ForTheme(grey, false), Is.EqualTo(ThemeColours.ForTheme(ThemeColours.DefaultBaseColour, false)));
            Assert.That(ThemeColours.ForTheme(grey, true), Is.EqualTo(ThemeColours.ForTheme(ThemeColours.DefaultBaseColour, true)));
        }

        [Test]
        public void BaseHue_ColouredBase_KeepsItsHue()
        {
            var red = new Color32(0xE5, 0x39, 0x35, 255);
            Assert.That(ThemeColours.BaseHue(red), Is.EqualTo(ThemeColours.ToLch(red).hue));
        }

        /// <summary>
        ///     CIE76 colour difference, the distance between two colours in CIE Lab.
        /// </summary>
        private static double DeltaE(Color32 first, Color32 second)
        {
            var a = ThemeColours.ToLch(first);
            var b = ThemeColours.ToLch(second);
            var hueA = a.hue * Math.PI / 180;
            var hueB = b.hue * Math.PI / 180;
            var labA = a.chroma * Math.Cos(hueA) - b.chroma * Math.Cos(hueB);
            var labB = a.chroma * Math.Sin(hueA) - b.chroma * Math.Sin(hueB);
            return Math.Sqrt((a.lightness - b.lightness) * (a.lightness - b.lightness) + labA * labA + labB * labB);
        }
    }
}