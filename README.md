# Asset Bundle Builder

This tool provides a workflow for managing and automating the process of building, uploading, and deploying asset bundles for Unity 3D games. It extends the built-in Unity tools be defining a convention for how bundles are named, where they are stored, how they are tracked, and how they are downloaded, such that it's easy to continually release updates to asset bundles for a live game.

## Motivation and Use Cases

The goal of this system is to make it easy to continuously update and deploy asset bundles for a live game. The default naming convention and build process provided by Unity is generic and doesn't impose many restrictions on how you manage asset bundles for your game, but that also means that common usage patterns have to be re-invented for each new project. This package attempts to provide a streamlined process that covers some common use cases:

- You want to iterate on and release updates to asset bundles without having to release an update to your game.
- Your game has a server component which can provide your game client with an updated list of asset bundle definitions.
- You would like to be able to automate the build and upload process for asset bundles as part of your CI process.
- Storage for built bundles isn't an issue (i.e. you're using Amazon S3 or some other cloud storage service), but you would like to limit when new bundles are deployed to minimize how often players need to download updated bundles.

> NOTE: If you are starting a new Unity project and are considering using asset-bundle-builder, consider using Unity's newer [Addressable Asset System](https://blogs.unity3d.com/2019/07/15/addressable-asset-system/) instead. This system is mainly intended for projects that are already using asset bundles and are heavily invested in the asset bundle workflow.

## Setup and Usage

To include asset-bundle-builder as a Unity package, you'll need to be on Unity 2018.3 or later. Open `Packages/manifest.json` in your project and add "com.synapse-games.asset-bundle-builder" to the `dependencies` object:

```json
{
  "dependencies": {
    "com.synapse-games.asset-bundle-builder": "https://github.com/kongregate/asset-bundle-builder.git#v0.2.0"
  }
}
```

> NOTE: You'll need to have Git installed on your development machine for Unity to be able to download the dependency. See https://git-scm.com/ for more information. Alternatively, you can clone the project directly into your `Packages` folder in order to vendor the package with your project.

> NOTE: If you're using an older version of Unity, you can still use bundle-builder by copying the contents of `Plugins` into your project's `Plugins` folder.

### Building and Uploading Bundles

Asset bundles are defined as normal in Unity, i.e. by tagging assets as part of a bundle. To build bundles, invoke `AssetBundleBuilder.BuildAssetBundles()`. This will build asset bundles to a folder `AssetBundles/<Platform>` in your project directory, then copy and rename the bundles into `AssetBundles/Staging`. The `Staging` directory will contain the current set of asset bundles for any built platforms.

Once you have built all of your asset bundles, call `AssetBundleBuilder.PrepareBundlesForUpload()`, passing it the URL of the web server where you store your built bundles. This will check for the presence of each bundle, and copy any bundles that haven't already been uploaded to `AssetBundles/Upload`. You should then upload any bundles in `AssetBundles/Upload` to your storage server or CDN as appropriate.

> NOTE: If you do not have your asset bundles in a publicly-accessible server, or are otherwise not able to check for the presence of bundles at build time, you can skip using `PrepareBundlesForUpload()`. In this case, you can upload bundles from `AssetBundles/Staging` directly, or perform any necessary custom logic to determine which bundles need to be uploaded.

Diagram of the `AssetBundles` directory:

```txt
AssetBundles/
├── <Platform>/
│   └── <asset bundles and manifest files>
├── Staging/
│   └── <merged and renamed asset bundles>
└── Upload/
    └── <asset bundles that haven't been uploaded yet>
```

### Generating the Bundle Descriptions

To generate the list of asset bundle descriptions used on the platform server, call `AssetBundleBuilder.GenerateBundleDescriptions()`. This will return a list of `AssetBundleDescription` objects which contain the information needed to load each bundle at runtime. This information needs to then be exported in some format that can be used by your game's server to tell your game's client which asset bundles to download.

While this package doesn't enforce any specific format for this data, it does provide support for using Json.NET to serialize the bundle descriptions to a JSON file. To set this up, you'll need to do the following:

* Add the [jillejr.newtonsoft.json-for-unity](https://www.npmjs.com/package/jillejr.newtonsoft.json-for-unity) package to your project.
* Add a `JSON_NET` definition to your project's [custom scripts defines](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html) in order to enable the Json.NET compatibility features in asset-bundle-builder.
* Register the `AssetBundleDescriptionConverter` class when performing serialization/deserialization:

```c#
var descriptions = AssetBundleBuilder
	.GenerateBundleDescriptions(new BuildTarget[] { ... });

var bundleJson = JsonConvert.SerializeObject(
	descriptions,
	Formatting.Indented,
	new AssetBundleDescriptionConverter());

File.WriteAllText(
    Path.Combine(AssetBundleBuilder.BundleOutputPath, "assetbundles.json"),
    bundleJson);
```

If not using JSON encoding (or not using Json.NET for the encoding), you will need to implement serialization for `AssetBundleDescription` using whatever system is appropriate for your game.

### Preparing Embedded Bundles

If you would like to distribute any asset bundles with your client directly, you can use `AssetBundleBuilder.CopyEmbeddedBundles()` to copy a set of bundles to `StreamingAssets/EmbeddedAssetBundles`. Only asset bundles for the specified target platform (defaulting to the currently-selected platform) will be copied over, to avoid bloating the built player with unused files.

Notes that you will have to manually rebuild asset bundles before calling `CopyEmbeddedBundles()`. Additionally, you'll need to make sure to build asset bundles and then call `CopyEmbeddedBundles()` before building your player in order to ensure that you are including the right version of any embedded bundles in your player.

### Downloading Bundles

To determine which asset bundles need to be downloaded at runtime, your game client should load the generated bundle description list (usually as a JSON document). Once loaded, the bundle description list will define the full set of bundles available to the client, including the information needed to identify the current version of a bundle for a given platform.

In general, the easiest way to determine the name of the file to download is the `FileNameForCurrentTarget` property. If a bundle was baked into your client build (i.e. by specifying it when calling `CopyEmbeddedBundles()`), you should instead use the `EmbeddedPath` property to get the full path to the embedded copy of the bundle.

> NOTE: This package doesn't provided any utilities for determining the full URL of your hosted bundles, nor does it provide any specific utilities for downloading, loading, or managing your asset bundles at runtime.

## Additional Documentation

This section more thoroughly explains the specifics of how asset-bundle-builder manages the asset bundles in your project.

### Asset Bundle File Naming

The generated bundle files are named according to the pattern `{name}_{platform}_{hash}.unity3d`, where:

* `{name}` is the bundle name, as defined in the Unity project.
* `{platform}` is the target platform string, matching the normalized variant names for [`RuntimePlatform`](https://docs.unity3d.com/ScriptReference/RuntimePlatform.html) (see [Platform Support](#platform-support) below).
* `{hash}` is the asset hash of the built bundle, determined when the bundle is built.

This naming conventions serves a number of purposes:

* Including the platform name in the file name allows the different platform-specific versions of a bundle to exist in the same folder. This simplifies the deployment of bundles and reduces complications when uploading/downloading bundles.
* Including the hash in the file name allows the different historical versions of the same bundle to be stored in the same directory in the CDN, which is necessary to support iteration and re-deployment of new versions of an asset bundle over time.
* Adding an explicit file extension makes the bundles play better with applications that expect all files to have extensions, and makes it easier for humans to tell what purpose the files serve.

### Platform Support

Unity uses the `BuildTarget` enum when building asset bundles and the `RuntimePlatform` enum to specify the current platform at runtime. Unfortunately, neither of these accurately reflects the set of platform-specific asset bundles that need to be built.  To address this, asset-bundle-builder uses a normalized subset of both `BuildTarget` and `RuntimePlatform` to identify the target platform for built asset bundles.

When building asset bundles, the specified `BuildTarget` will be normalized to account for cases where Unity differentiates between build targets at build time but not runtime. Specifically, when building for Windows there are the `StandaloneWindows` and `StandaloneWindows64` targets, however both will use the `WindowsPlayer` runtime platform. As such, asset-bundle-builder will only ever use the `StandaloneWindows` target when building asset bundles.

At runtime, the "Editor" variants for Windows, OSX, and Linux are normalized to the corresponding platform player variants, e.g. `WindowsEditor` will load bundles for `WindowsPlayer`. This is because the editor uses asset bundles for the corresponding platform's player.

Additionally, obsolete build targets and runtime platforms are not supported.

> NOTE: Not all platforms are currently supported, and there are some outstanding questions that need to be answered in order to properly handle asset bundles on all platforms. See [this thread on the Unity forums](https://forum.unity.com/threads/do-macos-and-windows-need-different-asset-bundles.670510/) for more details.

### Hosting Asset Bundles

Built bundles are expected to be hosted in a single folder on a remote server or CDN. This includes both the live version of any given bundle as well as any unpublished versions still in development. At any given time, the full set of asset bundles generated from the project can be uploaded to your hosting server without risking existing bundles being overwritten or broken.

>  NOTE: This package doesn't strictly require or enforce this convention for hosting your asset bundles, but it does provide a default workflow to support it. As such, following this convention will ensure a smoother build and deployment process for your game's asset bundles.

### Bundle Description List

This package supports generating a list of "bundle description" objects which specify the necessary information for loading your asset bundles at runtime. It also provides out-of-the-box functionality for converting this list to and from a JSON document for use at runtime.

For each asset bundle defined in your project, the bundle description will contain:

* The name of the bundle.
* The set of supported platforms and the corresponding asset hash for each one.
* The list of direct dependencies for each bundle.

This is the information needed at runtime to determine which bundles your game's client should download and to determine the correct filename to download for a given bundle. The `AssetBundleDescription` class provides utilities for accessing this information.

#### Building Bundle Descriptions

When building bundle descriptions, you need to provide the [`AssetBundleManifest`](https://docs.unity3d.com/ScriptReference/AssetBundleManifest.html) for each platform that you built bundles for. There are two ways to get the manifest after building bundles:

* `AssetBundleBuilder.BuildAssetBundles()` will return the manifest for each bundle built. If you are building all bundles at once using `BuildAssetBundles()`, you can pass the returned bundles directly into `MergePlatformManifests()`.
* Load the manifest from the manifest bundle generated by Unity. When building asset bundles for a platform, Unity produces an additional asset bundle that only contains the `AssetBundleManifest` for the build. You can either load the manifest from the bundle directly, or you can pass `MergePlatformManifests()` the paths to the bundles and it will load the manifests for you.

If you are building all of your asset bundles at the same time with a single call to `BuildAssetBundles()`, the first approach is recommended.

When using a build automation server, though, it is common to have multiple platforms build in parallel with separate copies of the project. In this case, no one workspace will have access to all generated bundle manifests. To generated the asset bundle descriptions, you'll have to setup your build system to copy the manifest bundles to a single workspace after all bundles finish building, and have a single workspace generate the bundle descriptions in a single build step.

#### JSON Conversion

Written out to JSON using the provided Json.NET conversion it would looks like this:

```json
[
    {
        "name": "bundle-1",
        "hashes": {
            "Android": "84dd10474d639ecc804d8fa3088887a2",
            "iOS": "4c92bf45a5c9e0281254cc7ad07690e9"
        },
        "dependencies": []
    },
    {
        "name": "cool-stuff",
        "hashes": {
            "Android": "301c62868060808f10b236e1de1dfaa8",
            "iOS": "90c94041de4ee425eb9ab23b9aa96021"
        },
        "dependencies": ["bundle-1"]
    }
]
```

#### Differences From `AssetBundleManifest`

Unity already provides the `AssetBundleManifest` asset to determine the hash and dependencies for each bundle, so why provide a separate system for providing that data at runtime? The primary reason for this is that `AssetBundleManifest` always represents the full set of asset bundles as contained in your Unity project when you build your bundles. This means that you can't choose when individual bundles are deployed: If you push the latest bundle manifest, all of your asset bundles are now live. Using a less opaque data format like JSON allows you to maintain a separate list of live bundles and manually choose when to move each bundle into production. Keeping information for all platforms in one file, rather than having a separate file per platform, further eases this process.

### Compatibility with Addressables

In July of 2019 Unity moved their [Addressable Asset System](https://blogs.unity3d.com/2019/07/15/addressable-asset-system/) out of preview status. This system is designed to replace asset bundles, and seeks to address many of the same use cases as asset-bundle-builder. Unfortunately, this package is not currently compatible with addressables, and it is not clear if compatibility is possible. If you believe you have a solution for migrating projects using asset-bundle-builder to the addressable asset system, please [open an issue](https://github.com/kongregate/asset-bundle-builder/issues/new) with your suggestion so that we can discuss integrating that functionality in the package!
