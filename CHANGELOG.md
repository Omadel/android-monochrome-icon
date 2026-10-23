# Changelog

All notable changes to this package are documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the package uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Package skeleton: manifest, editor assembly and test assembly.
- Adaptive icon XML edit that adds the monochrome layer, without duplicates.
- Project Settings page (Project › Android Monochrome Icon): add the layer or not, its image, fail the build on error. Settings saved in `ProjectSettings/AndroidMonochromeIconSettings.asset`.
- Live checks of the image under its field. Errors: no image, not a PNG file, not imported as Single Channel Alpha, not square, visible pixels not pure white. Warnings: not 432 px, no adaptive icon in the Android Player Settings.
- Apply Import Settings button, which imports the image as Single Channel from its Alpha channel.
- Build step for the generated Gradle project, after the same checks: the image copied as it is to `mipmap-xxxhdpi/app_icon_monochrome.png` of the launcher module, and a `<monochrome>` layer in `app_icon.xml` and `app_icon_round.xml`. An error fails the build if the settings ask for it, otherwise it is logged as a warning and the icon is built without the layer; warnings never fail the build. A warning when the Gradle project has no adaptive icon.
