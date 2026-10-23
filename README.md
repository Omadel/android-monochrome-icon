# Android Monochrome Icon

A Unity editor package that adds the **monochrome layer** to the adaptive icon of Android builds. Android 13 and later use this layer for **themed icons**, and Unity 6 does not write it.

The package comes with a Project Settings page that:

- sets the image of the layer;
- checks the image while you work on it, and again at build time;
- previews the themed icon in every Android mask shape, in the light and dark themes.

## Requirements

- Unity 6000.0 or later, with Android Build Support. The package is verified on Unity 6000.3.
- Adaptive icons set in the Android Player Settings (Player › Android › Icon › Adaptive): the monochrome layer is added to them.

On **Unity 7000** and later, Unity has its own setting for this layer (`adaptiveIconVectorDrawableMonochrome`). Use that setting there, and turn **Add Monochrome Layer** off, so that only one of them writes the layer. This package has not been tested with Unity 7000.

## Installation

In the Package Manager, choose **+ › Install package from git URL…** and enter:

```
https://github.com/Omadel/android-monochrome-icon.git#v1.0.0
```

Or add this line to the `dependencies` of `Packages/manifest.json`:

```json
"com.javienne.android-monochrome-icon": "https://github.com/Omadel/android-monochrome-icon.git#v1.0.0"
```

To update, change the tag at the end of the URL. The Package Manager does not list the versions of a git package.

## Quick start

1. Draw the layer: a square PNG, **white on a transparent background**, 432 × 432 px. Keep the drawing in the central 264 px, the 66 dp safe zone of adaptive icons.
2. Open **Edit › Project Settings › Android Monochrome Icon**, and set the PNG as the **Image**.
3. Click **Apply Import Settings** if the page asks for it.
4. Build for Android.

## The settings page

| Setting | Default | What it does |
| --- | --- | --- |
| Add Monochrome Layer | On | Adds the image as the monochrome layer in Android builds. |
| Fail Build On Error | Off | Fails the build when the image has an error. Otherwise, the error is logged as a warning and the icon is built without the monochrome layer. |
| Image | None | The PNG file of the layer. |
| Base Colour | `#1B6EF3` | Previews only: the colour the theme colours are derived from. |
| Extra Shapes | None | Previews only: a folder of extra mask shapes, see [Previews](#previews). |

All settings except Base Colour are saved in `ProjectSettings/AndroidMonochromeIconSettings.asset`, so commit that file with your project. Base Colour is saved in your editor preferences: it belongs to you, not to the project.

## Image rules

The page checks the image under its field whenever the image, its file or its import settings change. The build runs the same checks again.

**Errors**: the build leaves the layer out, or fails if **Fail Build On Error** is on.

- **No image is set.**
- **The file is not a PNG.** The build copies the file as it is, and Android needs a PNG.
- **The texture is not imported as Single Channel, from its Alpha channel.** The **Apply Import Settings** button sets this. The build does not read the imported texture, so this setting does not change the build: it keeps in the editor only what Android keeps.
- **The image is not square.**
- **Visible pixels are not pure white.** Every pixel with an opacity above zero must be white (255, 255, 255). Fully transparent pixels can have any colour.

**Warnings**: these never fail the build.

- **The image is not 432 px wide.** 432 px is the 108 dp layer at the xxxhdpi density. Android scales the image to the density of the device whatever its size.
- **The project has no adaptive icon.** The page reads the Android Player Settings; the build looks for the adaptive icon files of the Gradle project.

## What the build does

After Unity generates the Gradle project, the package runs the checks again. If the image has no error, the package then:

- copies the PNG file as it is to `launcher/src/main/res/mipmap-xxxhdpi/app_icon_monochrome.png`;
- adds `<monochrome android:drawable="@mipmap/app_icon_monochrome"/>` to `mipmap-anydpi-v26/app_icon.xml` and `app_icon_round.xml`, unless they already have a monochrome layer.

Only the xxxhdpi density is written: Android scales the image down for the other densities.

## How Android uses the layer

- On Android 13 (API 33) and later, when the user turns themed icons on, the launcher draws the monochrome layer in place of the icon, **tinted to the colours of the theme**.
- Android keeps only the **opacity** of the layer and discards its colour. A partly transparent pixel gives a lighter tint.
- The layer is 108 dp, like the foreground layer. The mask shows its central 72 dp, and the 66 dp safe zone stays visible in every shape.
- Android 8 to 12L (API 26 to 32) ignore the monochrome layer, and draw the adaptive icon as before.
- On Android 14 and later, the themed icon colours of AOSP come from the wallpaper, or from the colour chosen in the wallpaper and style settings:
  - light theme: background of primary tone 90, drawing of primary tone 30;
  - dark theme: background of secondary tone 20, drawing of primary tone 80.
- Device makers may use other colours and shapes.

## Previews

Under the checks, the page draws the image as a themed icon in each mask shape, in the light theme and in the dark theme. Hover a preview to see the name of its shape.

### Colours

The colours are an **approximation** of Android's colour scheme, in CIE LCh:

- each colour takes the tone of its role as its lightness L\*;
- it takes the hue of the base colour;
- its chroma is fixed per role, set to match the default blue.

They stay close to Android for the default blue (ΔE76 of 2 to 4), but drift for other hues: up to about 27 for the light background of a yellow base colour, and about 17 for a green one. Android computes these colours in its own colour model, HCT, which this package does not reproduce. A base colour too grey to have a hue (LCh chroma below 5) is replaced by the default blue, as Android does.

### Shapes

A shape is a **PNG file**:

- its opaque part is the inside of the mask;
- the image covers the 72 dp that the mask shows, and is stretched if it is not square;
- its file name is the name of the shape.

The package brings the 14 shapes found in the source code of Android (AOSP), as 288 px files (72 dp at xxxhdpi) in its `Shapes` folder:

| Shapes | Source | Android versions |
| --- | --- | --- |
| Default | Framework default mask (`config_icon_mask`) | 8 and later, unless the device sets another |
| Circle, Square, Four-Sided Cookie, Seven-Sided Cookie, Arch | Launcher3 shapes (`ShapesProvider`) | 16 QPR2 and later, chosen by the user |
| Squircle, Rounded Rectangle, Teardrop, Square | AOSP shape overlays | 10 and 11 |
| Pebble, Tapered Rectangle, Vessel, Heart | AOSP shape overlays | 11 |

The Pixel launcher is not open source: its shapes are those of Launcher3.

To preview other shapes, put PNG files in a folder of your project and set it as **Extra Shapes**. While the page is open, the folder and its subfolders are read again about once per second, so added, changed and removed files show up on their own. Their shapes come after those of the package. The page names any file that is not a readable PNG.

## Limits

- The package writes the layer at a single density, xxxhdpi.
- It copies the image as it is: it does not convert or generate it.
- It only edits the adaptive icon files that Unity generates, `app_icon.xml` and `app_icon_round.xml` in the `launcher` module. A Gradle setup that renames or moves them gets the "no adaptive icon" warning, and no layer.
- The previews are approximations. They do not simulate a launcher: no icon scaling, shadow or label.
- Verified in an Android App Bundle built with Unity 6000.3. Not verified on a device below Android 13.

## Tests

The Edit Mode tests are in `Tests/Editor`. To run them from a project, add the package to the `testables` of `Packages/manifest.json`:

```json
"testables": ["com.javienne.android-monochrome-icon"]
```

## License

MIT, see [LICENSE](LICENSE).