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
    /// <summary>
    /// Functionality for building and preparing asset bundles.
    /// </summary>
    public static class AssetBundleBuilder
    {
        /// <summary>
        /// The path to the asset bundle build folder, relative to the root directory
        /// of the Unity project.
        /// </summary>
        public static readonly string RootBuildPath = "AssetBundles";

        /// <summary>
        /// The path to the asset bundle staging directory, relative to the root
        /// directory of the Unity project.
        /// </summary>
        public static readonly string StagingArea = Path.Combine(RootBuildPath, "Staging");

        /// <summary>
        /// The path to the asset bundle upload directory, relative to the root directory
        /// of the Unity project.
        /// </summary>
        public static readonly string UploadArea = Path.Combine(RootBuildPath, "Upload");

        /// <summary>
        /// The path to the directory where embedded asset bundles are stored within the
        /// Unity project.
        /// </summary>
        ///
        /// <remarks>
        /// This path is for use in the editor. For accessing embedded bundles at runtime,
        /// use <see cref="AssetBundleDescription.EmbeddedPath"/>.
        /// </remarks>
        public static readonly string EmbeddedBundlePath =
            "Assets/StreamingAssets/EmbeddedAssetBundles";

        /// <summary>
        /// Builds asset bundles for the specified build target.
        /// </summary>
        ///
        /// <param name="buildTarget">
        /// The build target to build asset bundles for. Defaults to the active build
        /// target as specified by <see cref="EditorUserBuildSettings.activeBuildTarget"/>.
        /// Note that this will be normalized according to the rules specified by
        /// <see cref="NormalizeBuildTarget(BuildTarget)"/>.
        /// </param>
        ///
        /// <param name="options">
        /// Build options for the asset bundles. Defaults to <see cref="BuildAssetBundleOptions.None"/>.
        /// </param>
        ///
        /// <returns>The asset bundle manifest for the built bundles.</returns>
        ///
        /// <remarks>
        /// <para>
        /// The resulting bundles will be output to a directory under <see cref="RootBuildPath"/>
        /// named after <paramref name="buildTarget"/>. Due to build target normalization,
        /// this may not be the exact build target specified.
        /// </para>
        ///
        /// <para>
        /// After bundles are built, the set of bundles for the target platform are
        /// copied to the staging area and renamed according to the file naming
        /// convention. Note that the staging area is cleared at the beginning of the
        /// build, so after the build finishes it will only contain bundles for the
        /// specified platform.
        /// </para>
        /// </remarks>
        public static AssetBundleManifest BuildAssetBundles(
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

            // TODO: Delete any old bundles that have since been deleted from the project.

            // Clear out the staging area, if it already exists. This prevents a buildup
            // of old bundles over time.
            ResetDirectory(StagingArea);

            // Copy bundles to the staging area, renaming them based on the target
            // platform and asset hash.
            CopyBundlesToStagingArea(target, manifest);

            return manifest;
        }

        /// <summary>
        /// Builds asset bundles for all specified build targets.
        /// </summary>
        ///
        /// <param name="buildTargets">The set of build targets to build bundles for.</param>
        ///
        /// <param name="options">
        /// Build options for the asset bundles. Defaults to <see cref="BuildAssetBundleOptions.None"/>.
        /// </param>
        ///
        /// <returns>
        /// The set of manifests generated for each platform. Note that the keys of the
        /// dictionary are the normalized build targets, and so may not exactly match
        /// the list of targets in <paramref name="buildTargets"/>.
        /// </returns>
        ///
        /// <remarks>
        /// Behavior is the same as <see cref="BuildAssetBundles(BuildTarget?, BuildAssetBundleOptions)"/>,
        /// but will generate bundles for all specified platforms. The staging folder
        /// will be reset before the build, and will contain bundles for all specified
        /// platforms after the build finishes.
        /// </remarks>
        public static Dictionary<BuildTarget, AssetBundleManifest> BuildAssetBundles(
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

            // Clear out the staging area, if it already exists. This prevents a buildup
            // of old bundles over time.
            ResetDirectory(StagingArea);

            var manifests = new Dictionary<BuildTarget, AssetBundleManifest>();
            foreach (var buildTarget in buildTargets)
            {
                var target = NormalizeBuildTarget(buildTarget);

                // Generate asset bundles in a directory named based on the build target.
                var buildDirectory = GetBuildPathForBuildTarget(target);
                Directory.CreateDirectory(buildDirectory);

                // Invoke Unity's asset bundle build process.
                var manifest = BuildPipeline.BuildAssetBundles(buildDirectory, options, target);

                // TODO: Delete any old bundles that have since been deleted from the project.

                // Copy bundles to the staging area, renaming them based on the target
                // platform and asset hash.
                CopyBundlesToStagingArea(buildTarget, manifest);

                manifests.Add(target, manifest);
            }

            return manifests;
        }

        /// <summary>
        /// Copies built bundles to StreamingAssets to be embedded in the built player.
        /// </summary>
        ///
        /// <param name="embeddedBundles">
        /// A list of bundle names to copy. These should be the names of the asset
        /// bundles as defined in the Unity project. Will generate a warning if any of
        /// the specified names are invalid.
        /// </param>
        ///
        /// <param name="buildTarget">
        /// The build target to build asset bundles for. Defaults to the active build
        /// target as specified by <see cref="EditorUserBuildSettings.activeBuildTarget"/>.
        /// Note that this will be normalized according to the rules specified by
        /// <see cref="NormalizeBuildTarget(BuildTarget)"/>.
        /// </param>
        ///
        /// <remarks>
        /// <see cref="EmbeddedBundlePath"/> is reset each time this function is called.
        /// This is to ensure that asset bundles from another platform are not
        /// accidentally included when switching build targets. Only one platform's
        /// bundles may be embedded at a time since only one set of platform bundles
        /// will be valid for a built player.
        /// </remarks>
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

        /// <summary>
        /// Checks to see which asset bundles are already hosted, and copies any
        /// new/updated bundles to a separate folder for easy upload.
        /// </summary>
        ///
        /// <param name="baseUri">
        /// The address of the public server to check for the bundles. Server must be
        /// public (i.e. not require any authentication) and must accept HEAD requests.
        /// </param>
        ///
        /// <returns>
        /// An <see cref="EditorCoroutine"/> that will finish once all bundles have
        /// been copied.
        /// </returns>
        ///
        /// <remarks>
        /// In order to detect which bundles have already been uploaded we make a HEAD
        /// request to where the file would be stored relative to <paramref name="baseUri"/>.
        /// If the response code is 200 (or otherwise indicates success) then the bundle
        /// is considered to already be uploaded. If the response is 404 (or otherwise
        /// an error) then the bundle is considered not yet uploaded.
        /// </remarks>
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

                foreach (var bundlePath in Directory.GetFiles(StagingArea))
                {
                    var fileName = Path.GetFileName(bundlePath);
                    var uri = $"{_baseUri}/{fileName}";

                    // TODO: Execute network requests in parallel. This may be easier to
                    // do with async/await than with coroutines.
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

        /// <summary>
        /// Generates the list of asset bundle descriptions for the specified platforms.
        /// </summary>
        ///
        /// <param name="buildTargets">
        /// The target platforms to generate bundle descriptions for. Should be the full
        /// set of platforms your game supports.
        /// </param>
        ///
        /// <param name="options">
        /// Optional build parameters to use when performing the dry-run build. Should
        /// match the build options used when building bundles.
        /// </param>
        ///
        /// <returns>
        /// The bundle description for each of the bundles defined in the project. The
        /// bundle descriptions will include asset hashes for the platforms specified in
        /// <paramref name="buildTargets"/>. Note that the final set of platforms used
        /// will be normalized, and so may not exactly match the list specified.
        /// </returns>
        ///
        /// <remarks>
        /// Runs a dry-run build for the specified platforms in order to get the
        /// <see cref="AssetBundleManifest"/> for each platform.
        /// </remarks>
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

            var descriptions = new Dictionary<string, (Dictionary<AssetBundleTarget, Hash128> hashes, HashSet<string> dependencies)>();
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
                    if (!descriptions.TryGetValue(bundleName, out var description))
                    {
                        // The first time we create the list of description objects,
                        // popuplate the set of dependencies.
                        var dependencies = new HashSet<string>(
                            manifest.GetDirectDependencies(bundleName));

                        description = (new Dictionary<AssetBundleTarget, Hash128>(), dependencies);
                        descriptions.Add(bundleName, description);
                    }

                    // Set the hash for the current build target.
                    var bundleTarget = GetBundleTarget(target);
                    description.hashes[bundleTarget] = manifest.GetAssetBundleHash(bundleName);
                }
            }

            return descriptions
                .Select(pair => new AssetBundleDescription(pair.Key, pair.Value.hashes, pair.Value.dependencies))
                .ToArray();
        }

        /// <summary>
        /// Gets the corresponding <see cref="AssetBundleTarget"/> for the specified <see cref="BuildTarget"/>.
        /// </summary>
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

                // TODO: Support for other platforms.

                default:
                    throw new ArgumentException($"Cannot determine asset bundle target for unsupported build target {target}");
            }
        }

        /// <summary>
        /// Normalizes the target and appends it to <see cref="RootBuildPath"/>.
        /// </summary>
        private static string GetBuildPathForBuildTarget(BuildTarget target)
        {
            target = NormalizeBuildTarget(target);
            return Path.Combine(RootBuildPath, target.ToString());
        }

        /// <summary>
        /// Returns the normalized equivalent of the specified build target.
        /// </summary>
        ///
        /// <remarks>
        /// There are a handful of cases where two or more variants of <see cref="BuildTarget"/>
        /// use the same asset bundles at runtime, e.g. <see cref="BuildTarget.StandaloneWindows"/>
        /// and <see cref="BuildTarget.StandaloneWindows64"/>. In these cases, we define
        /// a single variant to be the "normalized" variant that is used when building
        /// bundles for all equivalent platforms. See the package documentation for the
        /// full set of conversions performed.
        /// </remarks>
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

        /// <summary>
        /// Ensures that a directory exists and is empty.
        /// </summary>
        private static void ResetDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Copies all bundles for the specified target to the staging area.
        /// </summary>
        ///
        /// <param name="buildTarget">
        /// The build target to use. Assumed to already be normalized.
        /// </param>
        ///
        /// <param name="manifest">
        /// The asset bundle manifest resulting from building asset bundles for the
        /// specified target.
        /// </param>
        private static void CopyBundlesToStagingArea(
            BuildTarget buildTarget,
            AssetBundleManifest manifest)
        {
            var buildDirectory = GetBuildPathForBuildTarget(buildTarget);
            var target = GetBundleTarget(buildTarget);

            foreach (var bundle in manifest.GetAllAssetBundles())
            {
                var sourceFile = Path.Combine(buildDirectory, bundle);
                var fileName = $"{bundle}_{target}_{manifest.GetAssetBundleHash(bundle)}.unity3d";

                File.Copy(
                    sourceFile,
                    Path.Combine(StagingArea, fileName),
                    true);
            }
        }
    }
}
