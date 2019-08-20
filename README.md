# AssetBundle Builder

This tool provides a workflow for managing and automating the process of building, uploading, and deploying asset bundles for Unity 3D games. It extends the built-in Unity tools be defining a convention for how bundles are named, where they are stored, how they are tracked, and how they are downloaded, such that it's easy to continually release updates to asset bundles for a live game.

## Usage

### Building and Uploading Bundles

Asset bundles are defined as normal in Unity (by tagging assets as part of a bundle). To build bundles, invoke `AssetBundleBuilder.BuildAssetBundles()`. This will build asset bundles to a folder `AssetBundles/<Platform>` in your project directory, then copy and rename the bundles into `AssetBundles/Staging`.

Once you have built all of your asset bundles, call `AssetBundleBuilder.PrepareBundlesForUpload()`, passing it the URL of the web server where you store your built bundles. This will check for the presence of each bundle, and copy any bundles that haven't already been uploaded to `AssetBundles/Upload`. You should then upload any bundles in `AssetBundles/Upload` to your storage server or CDN as appropriate.

### Generating the Bundle Description JSON

> TODO: Describe the purpose and definitions for the JSON data.

### Downloading Bundles

> TODO: Describe how a server implementation should handle the bundle JSON, and how a client should use that data to download bundles.

## Motivation and Use Cases

The goal of this system is to make it easy to continuously update and deploy asset bundles for a live game. The default naming convention and build process provided by Unity is generic and doesn't impose many restrictions on how you manage asset bundles for your game, but that also means that common usage patterns have to be re-invented for each new project. This package attempts to provide a streamlined process that covers some common use cases:

* You want to iterate on and release updates to asset bundles without having to release an update to your game.
* Your game has a server component which can provide your game client with an updated list of asset bundle definitions.
* You would like to be able to automate the build and upload process for asset bundles as part of your CI process.
* Storage for built bundles isn't an issue (i.e. you're using Amazon S3 or some other cloud storage service), but you would like to limit when new bundles are deployed to minimize how often players need to download updated bundles.

## Specification

* Asset bundles are given a unique name matching the name of the bundle specified in the Unity editor.
* The generated bundle files are named according to the pattern `{name}_{platform}_{hash}.unity3d`, where:

  * `{name}` is the bundle name, as defined in the Unity project.
  * `{platform}` is the target platform string, matching the variant names for [`BuildTarget`](https://docs.unity3d.com/ScriptReference/BuildTarget.html).
  * `{hash}` is the asset hash of the built bundle, determined when the bundle is built.

* For platforms with multiple target architectures, bundles are only built once for all architectures. The platform name used is always the base platform name without an architecture-specific suffix (e.g. "StandaloneWindows" is used for both the `StandaloneWindows` and `StandaloneWindows64` build targets).
* Built bundles are stored in a single folder in the CDN. This includes the live version of any given bundle, as well as any unpublished versions still in development.
* The bundle description JSON determines which version of a bundle (specified by the platform-specific asset hash) is live at any given time.
* The build process generates a dev bundle description file containing the current hashes for all bundles bundles. New versions of a bundle are deployed by moving the latest hashes over from the dev description file to the live description file.
* When specifying the set of live bundles the client, the server may either directly provide the client with the bundle description JSON, or it may process the description JSON and provide the client with data in a different format to meet game specific needs.
  * If the bundle description JSON is passed directly, the client must know the base URL where bundles are stored so that it can put together the final URL for the bundles.
  * If the server sends the client custom data, it must provide the following information in order to for the client to be able to download the bundles:
    * Either the full filename for the bundle, or separately provide the name, platform string, and hash so that the client can construct the file name itself.
    * The asset hash for the client's target platform. This is necessary so that client can know if it already has the bundle cached locally or not.

### Bundle Description JSON

Example:

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

