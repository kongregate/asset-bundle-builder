using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SynapseGames.AssetBundle
{
    public static class AssetBundleBuilder
    {
        public static readonly string BundleOutputPath = "AssetBundles";

        public static readonly string BundleStagingArea = Path.Combine(BundleOutputPath, "Staging");

        public static readonly string UploadArea = Path.Combine(BundleOutputPath, "Upload");

        public static readonly string EmbeddedBundlePath = "Assets/StreamingAssets/EmbeddedAssetBundles";

        public static void BuildAssetBundles(
            BuildTarget? buildTarget = null,
            BuildAssetBundleOptions options = BuildAssetBundleOptions.None)
        {
            var target = buildTarget ?? EditorUserBuildSettings.activeBuildTarget;
            target = NormalizeBuildTarget(target);

            // Generate asset bundles in a directory named based on the build target.
            var buildDirectory = GetBuildPathForBuildTarget(target);
            Directory.CreateDirectory(buildDirectory);

            // Invoke Unity's asset bundle build process.
            var manifest = BuildPipeline.BuildAssetBundles(buildDirectory, options, target);

            // Clear out the staging area, if it already exists. This prevents a buildup
            // of old bundles over time.
            ResetDirectory(BundleStagingArea);

            // Copy bundles to the staging area, renaming them based on the target
            // platform and asset hash.
            foreach (var bundle in manifest.GetAllAssetBundles())
            {
                var sourceFile = Path.Combine(buildDirectory, bundle);
                var fileName = $"{bundle}_{target}_{manifest.GetAssetBundleHash(bundle)}.unity3d";
                File.Copy(
                    sourceFile,
                    Path.Combine(BundleStagingArea, fileName),
                    true);
            }
        }

        public static void CopyEmbeddedBundles(
            string[] embeddedBundles,
            BuildTarget? buildTarget = null)
        {
            var target = buildTarget ?? EditorUserBuildSettings.activeBuildTarget;
            target = NormalizeBuildTarget(target);

            var buildPath = GetBuildPathForBuildTarget(target);

            // Fully reset the embedded bundle path in order to clear out removed
            // bundles or bundles from other platforms.
            ResetDirectory(EmbeddedBundlePath);

            // Gather the set of valid bundle names in order to validate the names
            // passed by the user.
            var validBundleNames = new HashSet<string>(AssetDatabase.GetAllAssetBundleNames());

            foreach (var bundleName in embeddedBundles)
            {
                if (!validBundleNames.Contains(bundleName))
                {
                    Debug.LogWarning($"Unable to copy unknown embedded bundle {bundleName}");
                    continue;
                }

                var srcPath = Path.Combine(buildPath, bundleName);
                if (!File.Exists(srcPath))
                {
                    Debug.LogWarning(
                        $"Asset bundle {bundleName} has not been built for target {target}, " +
                        $"make sure to build bundles before calling CopyEmbeddedBundles()");
                    continue;
                }

                var destPath = Path.Combine(EmbeddedBundlePath, bundleName);
                File.Copy(srcPath, destPath, true);
            }
        }

        public static EditorCoroutine PrepareBundlesForUpload(string baseUri)
        {
            return EditorCoroutineUtility.StartCoroutineOwnerless(
                PrepareBundlesForUploadRoutine(baseUri));

            IEnumerator PrepareBundlesForUploadRoutine(string _baseUri)
            {
                // Clear out the upload area, if it already exists. This prevents a buildup
                // of bundles over time, and ensures that after a build the upload area will
                // only contain the latest bundles that need to be uploaded.
                ResetDirectory(UploadArea);

                foreach (var bundlePath in Directory.GetFiles(BundleStagingArea))
                {
                    var fileName = Path.GetFileName(bundlePath);
                    var uri = $"{_baseUri}/{fileName}";

                    // TODO: Execute network requests in parallel.
                    var request = UnityWebRequest.Head(uri);
                    yield return request.SendWebRequest();

                    // TODO: Why do we need to manually check if the request is done
                    // here? We should be able to just yield on the request and assume
                    // it's done once we resume the coroutine. For some reason, that
                    // doesn't seem to be working in the editor.
                    while (!request.isDone)
                    {
                        yield return null;
                    }

                    if (request.isHttpError)
                    {
                        var destPath = Path.Combine(UploadArea, fileName);
                        File.Copy(bundlePath, destPath, true);
                    }
                }
            }
        }

        public static AssetBundleDescription[] GenerateBundleDescriptions(
            BuildTarget[] buildTargets,
            BuildAssetBundleOptions options = BuildAssetBundleOptions.None)
        {
            // Normalize the list of build targets by normalizing the individual targets
            // specified and then removing any duplicates. For example, if the user
            // specifies both StandaloneWindows and StandaloneWindows64, we only want to
            // generate output for StandaloneWindows once.
            buildTargets = buildTargets
                .Select(NormalizeBuildTarget)
                .Distinct()
                .ToArray();

            var descriptions = new Dictionary<string, Dictionary<AssetBundleTarget, Hash128>>();
            foreach (var target in buildTargets)
            {
                var buildDirectory = GetBuildPathForBuildTarget(target);

                // Make sure the output directory for the platform exists.
                //
                // NOTE: This directory won't actually be populated because we're doing
                // a dry run, but Unity will still complain if the directory isn't present.
                Directory.CreateDirectory(buildDirectory);

                // Perform a dry run of the build in order to get the build manifest
                // without having to wait for the build to complete on all platforms.
                var manifest = BuildPipeline.BuildAssetBundles(
                    buildDirectory,
                    options | BuildAssetBundleOptions.DryRunBuild,
                    target);

                foreach (var bundleName in manifest.GetAllAssetBundles())
                {
                    // Get the existing description object for the current bundle, or
                    // create a new one and add it to the description dictionary.
                    Dictionary<AssetBundleTarget, Hash128> hashes;
                    if (!descriptions.TryGetValue(bundleName, out hashes))
                    {
                        hashes = new Dictionary<AssetBundleTarget, Hash128>();
                        descriptions.Add(bundleName, hashes);
                    }

                    // Set the hash for the current build target.
                    var bundleTarget = GetBundleTarget(target);
                    hashes[bundleTarget] = manifest.GetAssetBundleHash(bundleName);
                }
            }

            return descriptions
                .Select(pair => new AssetBundleDescription(pair.Key, pair.Value))
                .ToArray();
        }

        public static AssetBundleTarget GetBundleTarget(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return AssetBundleTarget.StandaloneWindows;

                case BuildTarget.StandaloneOSX:
                    return AssetBundleTarget.StandaloneOSX;

                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return AssetBundleTarget.StandaloneLinux;

                case BuildTarget.Android:
                    return AssetBundleTarget.Android;

                case BuildTarget.iOS:
                    return AssetBundleTarget.iOS;

                case BuildTarget.WebGL:
                    return AssetBundleTarget.WebGL;

                default:
                    throw new ArgumentException($"Cannot determine asset bundle target for unsupported build target {target}");
            }
        }

        private static string GetBuildPathForBuildTarget(BuildTarget target)
        {
            target = NormalizeBuildTarget(target);
            return Path.Combine(BundleOutputPath, target.ToString());
        }

        public static BuildTarget NormalizeBuildTarget(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return BuildTarget.StandaloneWindows;

                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return BuildTarget.StandaloneLinux;

                default:
                    return target;
            }
        }

        private static void ResetDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }
    }
}
