using System;
using UnityEngine;

namespace Javienne.AndroidMonochromeIcon
{
    /// <summary>
    ///     Approximate colours of a themed icon on Android 14 to 17, from the base colour the system derives them from. Each
    ///     colour role keeps its tone as the CIE L* lightness and takes the hue of the base colour in CIE LCh, with a fixed
    ///     chroma per role that matches the default blue. Android computes them in its own colour model, HCT, so the other
    ///     hues drift, the most for yellows and greens in the light theme.
    /// </summary>
    internal static class ThemeColours
    {
        private const double LIGHT_BACKGROUND_TONE = 90;
        private const double LIGHT_BACKGROUND_CHROMA = 15;
        private const double LIGHT_FOREGROUND_TONE = 30;
        private const double LIGHT_FOREGROUND_CHROMA = 28;
        private const double DARK_BACKGROUND_TONE = 20;
        private const double DARK_BACKGROUND_CHROMA = 12;
        private const double DARK_FOREGROUND_TONE = 80;
        private const double DARK_FOREGROUND_CHROMA = 22;
        private const double MIN_BASE_CHROMA = 5;
        private const double WHITE_X = 0.95047;
        private const double WHITE_Z = 1.08883;
        private const double LAB_EPSILON = 216.0 / 24389.0;
        private const double LAB_KAPPA = 24389.0 / 27.0;
        private const double GAMUT_TOLERANCE = 1e-4;
        private const int GAMUT_STEPS = 24;

        /// <summary>
        ///     Default base colour of Android, the blue of monet, which also replaces base colours too grey to give a hue.
        /// </summary>
        internal static Color32 DefaultBaseColour => new Color32(0x1B, 0x6E, 0xF3, 255);

        /// <summary>
        ///     Background and drawing colours of a themed icon in the light or dark theme: primary tones 90 and 30 in the
        ///     light theme, secondary tone 20 and primary tone 80 in the dark theme.
        /// </summary>
        internal static (Color32 background, Color32 foreground) ForTheme(Color32 baseColour, bool dark)
        {
            var hue = BaseHue(baseColour);
            return dark ? (FromLch(DARK_BACKGROUND_TONE, DARK_BACKGROUND_CHROMA, hue), FromLch(DARK_FOREGROUND_TONE, DARK_FOREGROUND_CHROMA, hue)) : (FromLch(LIGHT_BACKGROUND_TONE, LIGHT_BACKGROUND_CHROMA, hue), FromLch(LIGHT_FOREGROUND_TONE, LIGHT_FOREGROUND_CHROMA, hue));
        }

        /// <summary>
        ///     CIE LCh hue of the base colour, or of the default blue when the base colour is too grey to give one, as monet
        ///     does.
        /// </summary>
        internal static double BaseHue(Color32 baseColour)
        {
            var lch = ToLch(baseColour);
            return lch.chroma < MIN_BASE_CHROMA ? ToLch(DefaultBaseColour).hue : lch.hue;
        }

        /// <summary>
        ///     CIE LCh coordinates of an sRGB colour, under the D65 white point: lightness from 0 to 100, chroma, hue in degrees
        ///     from 0 to 360.
        /// </summary>
        internal static (double lightness, double chroma, double hue) ToLch(Color32 colour)
        {
            var r = ToLinear(colour.r);
            var g = ToLinear(colour.g);
            var b = ToLinear(colour.b);
            var fx = LabF((0.4124564 * r + 0.3575761 * g + 0.1804375 * b) / WHITE_X);
            var fy = LabF(0.2126729 * r + 0.7151522 * g + 0.0721750 * b);
            var fz = LabF((0.0193339 * r + 0.1191920 * g + 0.9503041 * b) / WHITE_Z);
            var labA = 500 * (fx - fy);
            var labB = 200 * (fy - fz);
            var hue = Math.Atan2(labB, labA) * 180 / Math.PI;
            return (116 * fy - 16, Math.Sqrt(labA * labA + labB * labB), hue < 0 ? hue + 360 : hue);
        }

        /// <summary>
        ///     sRGB colour of CIE LCh coordinates. Outside the sRGB gamut, the chroma is lowered until the colour fits, which
        ///     keeps its lightness and hue.
        /// </summary>
        internal static Color32 FromLch(double lightness, double chroma, double hue)
        {
            if (TryToLinearRgb(lightness, chroma, hue, out var rgb)) return ToColour(rgb);

            var low = 0.0;
            var high = chroma;
            for (var i = 0; i < GAMUT_STEPS; i++)
            {
                var middle = (low + high) / 2;
                if (TryToLinearRgb(lightness, middle, hue, out _)) low = middle;
                else high = middle;
            }

            TryToLinearRgb(lightness, low, hue, out rgb);
            return ToColour(rgb);
        }

        /// <summary>
        ///     Linear sRGB components of CIE LCh coordinates, and whether they are all inside the gamut.
        /// </summary>
        private static bool TryToLinearRgb(double lightness, double chroma, double hue, out (double r, double g, double b) rgb)
        {
            var radians = hue * Math.PI / 180;
            var fy = (lightness + 16) / 116;
            var fx = fy + chroma * Math.Cos(radians) / 500;
            var fz = fy - chroma * Math.Sin(radians) / 200;
            var x = WHITE_X * LabFInverse(fx);
            var y = LabFInverse(fy);
            var z = WHITE_Z * LabFInverse(fz);
            rgb = (3.2404542 * x - 1.5371385 * y - 0.4985314 * z, -0.9692660 * x + 1.8760108 * y + 0.0415560 * z, 0.0556434 * x - 0.2040259 * y + 1.0572252 * z);
            return IsInGamut(rgb.r) && IsInGamut(rgb.g) && IsInGamut(rgb.b);
        }

        private static bool IsInGamut(double component)
        {
            return component >= -GAMUT_TOLERANCE && component <= 1 + GAMUT_TOLERANCE;
        }

        private static double LabF(double t)
        {
            return t > LAB_EPSILON ? Math.Pow(t, 1.0 / 3.0) : (LAB_KAPPA * t + 16) / 116;
        }

        private static double LabFInverse(double f)
        {
            var cube = f * f * f;
            return cube > LAB_EPSILON ? cube : (116 * f - 16) / LAB_KAPPA;
        }

        private static double ToLinear(byte component)
        {
            var value = component / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        private static byte ToSrgb(double component)
        {
            var value = Math.Min(1, Math.Max(0, component));
            var encoded = value <= 0.0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - 0.055;
            return (byte)Math.Round(encoded * 255);
        }

        private static Color32 ToColour((double r, double g, double b) rgb)
        {
            return new Color32(ToSrgb(rgb.r), ToSrgb(rgb.g), ToSrgb(rgb.b), 255);
        }
    }
}