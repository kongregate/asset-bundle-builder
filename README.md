# AssetBundle Builder

This tool provides a workflow for managing and automating the process of building, uploading, and deploying asset bundles for Unity 3D games. It extends the built-in Unity tools be defining a convention for how bundles are named, where they are stored, how they are tracked, and how they are downloaded, such that it's easy to continually release updates to asset bundles for a live game.

## Motivation and Use Cases

The goal of this system is to make it easy to continuously update and deploy asset bundles for a live game. The default naming convention and build process provided by Unity is generic and doesn't impose many restrictions on how you manage asset bundles for your game, but that also means that common usage patterns have to be re-invented for each new project. This package attempts to provide a streamlined process that covers some common use cases:

- You want to iterate on and release updates to asset bundles without having to release an update to your game.
- Your game has a server component which can provide your game client with an updated list of asset bundle definitions.
- You would like to be able to automate the build and upload process for asset bundles as part of your CI process.
- Storage for built bundles isn't an issue (i.e. you're using Amazon S3 or some other cloud storage service), but you would like to limit when new bundles are deployed to minimize how often players need to download updated bundles.

## Setup and Usage

To include asset-bundle-builder as a Unity package, you'll need to be on Unity 2018.3 or later. Open `Packages/manifest.json` in your project and add "com.synapse-games.asset-bundle-builder" to the `dependencies` object:

```json
{
  "dependencies": {
    "com.synapse-games.asset-bundle-builder": "https://github.com/kongregate/asset-bundle-builder.git"
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

### Generating the Bundle Description JSON

To generate the list of asset bundle descriptions used on the platform server, call `AssetBundleBuilder.GenerateBundleDescriptions()`. This will return a list of `AssetBundleDescription` objects which contain the bundle name and platform-specific asset hashes for each platform. This information needs to then be exported in some format that can be used by your game's server to tell your game's client which asset bundles to download.

While this package doesn't enforce any specific format for this data, it does provide support for using Json.NET to serialize the bundle descriptions to a JSON file. Note that you'll need to use the `Hash128Converter` when doing this serialization in order to correctly serialize/deserialize Unity's `Hash128` type:

```c#
var descriptions = AssetBundleBuilder
	.GenerateBundleDescriptions(new BuildTarget[] { ... });

var bundleJson = JsonConvert.SerializeObject(
	descriptions,
	Formatting.Indented,
	new Hash128Converter());

File.WriteAllText(
    Path.Combine(AssetBundleBuilder.BundleOutputPath, "assetbundles.json"),
    bundleJson);
```

### Preparing Embedded Bundles

If you would like to distribute any asset bundles with your client directly, you can use `AssetBundleBuilder.CopyEmbeddedBundles()` to copy a set of bundles to `StreamingAssets/EmbeddedAssetBundles`. Only asset bundles for the specified target platform (defaulting to the currently-selected platform) will be copied over, to avoid bloating the built player with unused files.

Notes that you will have to manually rebuild asset bundles before calling `CopyEmbeddedBundles()`. Additionally, you'll need to make sure to build asset bundles and then call `CopyEmbeddedBundles()` before building your player in order to ensure that you are including the right version of any embedded bundles in your player.

### Downloading Bundles

To determine which asset bundles need to be downloaded at runtime, your game client should load the generated bundle description list (usually as a JSON document). Once loaded, the bundle description list will define the full set of bundles available to the client, including the information needed to identify the current version of a bundle for a given platform.

In general, the easiest way to determine the name of the file to download is the `FileNameForCurrentTarget` property. If a bundle was baked into your client build (i.e. by specifying it when calling `CopyEmbeddedBundles()`), you should instead use the `EmbeddedPath` property to get the full path to the embedded copy of the bundle.

> NOTE: This package doesn't provided any utilities for determining the full URL of your hosted bundles, nor does it provide any specific utilities for downloading, loading, or managing your asset bundles at runtime.

## Specification

The following are the specific details of how this package 

### AssetBundle File Naming

The generated bundle files are named according to the pattern `{name}_{platform}_{hash}.unity3d`, where:

* `{name}` is the bundle name, as defined in the Unity project.
* `{platform}` is the target platform string, matching the variant names for [`BuildTarget`](https://docs.unity3d.com/ScriptReference/BuildTarget.html).
* `{hash}` is the asset hash of the built bundle, determined when the bundle is built.

This naming conventions serves a number of purposes:

* Including the platform name in the file name allows the different platform-specific versions of a bundle to exist in the same folder. This simplifies the deployment of bundles and reduces complications when uploading/downloading bundles.
* Including the hash in the file name allows the different historical versions of the same bundle to be stored in the same directory in the CDN, which is necessary to support iteration and re-deployment of new versions of an asset bundle over time.
* Adding an explicit file extension makes the bundles play better with applications that expect all files to have extensions, and makes it easier for humans to tell what purpose the files serve.

### Platform Support

For platforms with multiple target architectures, bundles are only built once for all architectures. The platform name used is always the base platform name without an architecture-specific suffix (e.g. "StandaloneWindows" is used for both the `StandaloneWindows` and `StandaloneWindows64` build targets).

> NOTE: Not all platforms have been added to the `AssetBundleTarget` enum, and there are some outstanding questions that need to be answered in order to determine what constitutes a distinct "platform" for the purpose of building/loading asset bundles. See [this thread on the Unity forums](https://forum.unity.com/threads/do-macos-and-windows-need-different-asset-bundles.670510/) to follow the discussion.

### Hosting AssetBundles

Built bundles are stored in a single folder in the CDN. This includes the live version of any given bundle, as well as any unpublished versions still in development.

### Bundle Description List

- The bundle description JSON determines which version of a bundle (specified by the platform-specific asset hash) is live at any given time.
- The build process generates a dev bundle description file containing the current hashes for all bundles bundles. New versions of a bundle are deployed by moving the latest hashes over from the dev description file to the live description file.
- When specifying the set of live bundles the client, the server may either directly provide the client with the bundle description JSON, or it may process the description JSON and provide the client with data in a different format to meet game specific needs.
  - If the bundle description JSON is passed directly, the client must know the base URL where bundles are stored so that it can put together the final URL for the bundles.
  - If the server sends the client custom data, it must provide the following information in order to for the client to be able to download the bundles:
    - Either the full filename for the bundle, or separately provide the name, platform string, and hash so that the client can construct the file name itself.
    - The asset hash for the client's target platform. This is necessary so that client can know if it already has the bundle cached locally or not.

JSON example:

```json
[
    {
        "name": "bundle-1",
        "hashes": {
            "Android": "dvdvdvdvdvsdvasdvasdvav",
            "iOS": "aevasevasevasevasevasevasev"
        }
    },
    {
        "name": "cool-stuff",
        "hashes": {
            "Android": "asdfasdfasdf",
            "iOS": "asdfasdfasdfasdf"
        }
    }
]
```
