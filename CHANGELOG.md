# Changelog

All notable changes to this package are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-23

First release, for Unity 6000.0 and later.

### Added

- Build step for Android, after Unity generates the Gradle project:
  - copies the image as it is to `mipmap-xxxhdpi/app_icon_monochrome.png` of the `launcher` module;
  - adds a `<monochrome>` layer to `app_icon.xml` and `app_icon_round.xml`, once.
- Project Settings page, Project › Android Monochrome Icon: add the layer or not, its image, and fail the build on error. These settings are saved in `ProjectSettings/AndroidMonochromeIconSettings.asset`.
- Checks of the image, live on the settings page and again at build time:
  - errors: no image, not a PNG file, not imported as Single Channel Alpha, not square, visible pixels not pure white;
  - warnings: not 432 px, no adaptive icon.

  At build time, an error fails the build if the settings ask for it; otherwise it is logged as a warning, and the icon is built without the layer. Warnings never fail the build.
- Apply Import Settings button, which imports the image as Single Channel, from its Alpha channel.
- Themed icon previews on the settings page: the image in each mask shape, in the light and dark themes. Their colours are approximated in CIE LCh from a base colour, `#1B6EF3` by default, saved for each user in the editor preferences.
- Mask shapes as PNG files: the opaque part of a file is the inside of the mask, and the file name names the shape.
  - The package brings the 14 shapes found in AOSP: the default mask, the Launcher3 shapes of Android 16 QPR2, and the shape overlays of Android 10 and 11.
  - An Extra Shapes folder, set on the settings page, adds the project's own shapes.
- Edit Mode tests for the build step, the checks, the colours and the previews.

[1.0.0]: https://github.com/Omadel/android-monochrome-icon/releases/tag/v1.0.0