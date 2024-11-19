# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [v0.2.3]

### Fixed

* Updated `isHttpError` and Linux `BuildTargets` to recommended uses in 2024

### Added

* `PrepareBundlesForUpload()` can now use a short delay between requests
* Simple retry system for requests in `PrepareBundlesForUpload()`. This allows differentiating between cases where the server could not be reached
and where the bundle is not uploaded, throwing an exception in the former case, which helps resolve that ambiguity. Previously, if the server was unresponsive
the asset bundle would be marked for upload but the actual upload would fail.
* `PrepareBundlesForUpload()` logging

## [v0.2.2]

### Fixed

* `NormalizePlatform()` no longer throws an exception for "unsupported" platforms. ([#7])

[#7]: https://github.com/kongregate/asset-bundle-builder/pull/7

## [v0.2.1]

### Added

* `ManifestBundlePath`, `CurrentBuildPlatform`, and `GetManifestBundlePathForTarget()`. ([#6])

[#6]: https://github.com/kongregate/asset-bundle-builder/pull/6

## [v0.2.0]

### Added

* `MergePlatformManifests()` as a more robust way of generating the `AssetBundleDescription` objects after building asset bundles. ([#4])

### Fixed

* Build asset bundles are now placed in folders based on their corresponding `RuntimePlatform`, rather than their `BuildTarget`. ([#4])

### Breaking Changes

* `AssetBundleBuilder.GenerateBundleDescriptions()` has been removed and replaced with `MergePlatformManifests()`. You will need to update your build process to provide the `AssetBundleManifest` for each platform to `MergePlatformManifests()`. This can be done by either directly providing the `AssetBundleManifest` asset for each platform, or providing the path to each platform's manifest bundle. See the "Building Bundle Descriptions" section of the README for more information.

[#4]: https://github.com/kongregate/asset-bundle-builder/pull/4

## [v0.1.0] - 2019-11-08

Initial release :tada: Provides basic workflow for building, distributing, and loading asset bundles.

[Unreleased]: https://github.com/kongregate/asset-bundle-builder/compare/v0.2.3...master
[v0.2.3]: https://github.com/kongregate/asset-bundle-builder/compare/v0.2.2...v0.2.3
[v0.2.2]: https://github.com/kongregate/asset-bundle-builder/compare/v0.2.1...v0.2.2
[v0.2.1]: https://github.com/kongregate/asset-bundle-builder/compare/v0.2.0...v0.2.1
[v0.2.0]: https://github.com/kongregate/asset-bundle-builder/compare/v0.1.0...v0.2.0
[v0.1.0]: https://github.com/kongregate/asset-bundle-builder/compare/56f87b9...v0.1.0
