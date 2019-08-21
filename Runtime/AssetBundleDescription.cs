using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SynapseGames.AssetBundle
{
    /// <summary>
    /// Data needed to load an asset bundle at runtime.
    /// </summary>
    ///
    /// <remarks>
    /// This type is intended to be loaded from JSON (or another intermediate data
    /// format) at runtime and used to determine what asset bundles need to be loaded.
    /// It provides enough information to determine the file name for a given asset
    /// bundle for any supported platform. The values are generated at build time using <see cref="AssetBundleBuilder"/>
    /// </remarks>
    public class AssetBundleDescription : IEquatable<AssetBundleDescription>
    {
        /// <summary>
        /// The name of the asset bundle, as specified in the Unity editor.
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary>
        /// The set of platform-specific asset hashes for the bundle.
        /// </summary>
        [JsonProperty("hashes")]
        public Dictionary<AssetBundleTarget, Hash128> Hashes = new Dictionary<AssetBundleTarget, Hash128>();

        /// <summary>
        /// The file name for the current platform, if any. Will be null if there is no
        /// hash for the current platform.
        /// </summary>
        public string FileNameForCurrentTarget => FileNameForTarget(CurrentTarget);

        /// <summary>
        /// The path to the asset bundle when it is distributed as an embedded bundle.
        /// </summary>
        ///
        /// <remarks>
        /// This path will be inside the streaming assets path, which may require
        /// platform-specific considerations when loading.
        /// </remarks>
        public string EmbeddedPath => Path.Combine(
            Application.streamingAssetsPath,
            "EmbeddedAssetBundles",
            Name);

        /// <summary>
        /// Initializes an empty <see cref="AssetBundleDescription"/>.
        /// </summary>
        ///
        /// <remarks>
        /// Intended primarily for use when deserializing data, since Json.NET prefers
        /// to have a default constructor.
        /// </remarks>
        public AssetBundleDescription() { }

        /// <summary>
        /// Initializes an <see cref="AssetBundleDescription"/> with a name but no hashes.
        /// </summary>
        /// <param name="name"></param>
        public AssetBundleDescription(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Fully initializes a new <see cref="AssetBundleDescription"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="hashes"></param>
        public AssetBundleDescription(string name, Dictionary<AssetBundleTarget, Hash128> hashes)
        {
            Name = name;
            Hashes = hashes;
        }

        /// <summary>
        /// Gets the filename for the bundle on the specified target platform.
        /// </summary>
        ///
        /// <param name="target">The target platform to get the asset bundle for.</param>
        ///
        /// <returns>
        /// The file name for the specified target platform, or null if no hash is
        /// present for that platform.
        /// </returns>
        public string FileNameForTarget(AssetBundleTarget target)
        {
            if (Hashes.TryGetValue(target, out var hash))
            {
                return $"{Name}_{target}_{hash}";
            }

            return null;
        }

        public override string ToString()
        {
            // TODO: There's probably a better way of converting this to a string than
            // just serializing it to JSON.
            return JsonConvert.SerializeObject(this, new Hash128Converter());
        }

        public override bool Equals(object obj)
        {
            if (obj is AssetBundleDescription other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = Name.GetHashCode();

            // Iterate over the variants of AssetBundleTarget in order to hash the values in the dictionary in a deterministic order.
            var variants = Enum.GetValues(typeof(AssetBundleTarget)).Cast<AssetBundleTarget>();
            foreach (var variant in variants)
            {
                Hash128 platformHash;
                if (Hashes.TryGetValue(variant, out platformHash))
                {
                    hash = (hash, variant, platformHash).GetHashCode();
                }
            }

            return hash;
        }

        public bool Equals(AssetBundleDescription other)
        {
            if (Name != other.Name)
            {
                return false;
            }

            if (Hashes == other.Hashes)
            {
                return true;
            }

            if (Hashes.Count != other.Hashes.Count)
            {
                return false;
            }

            foreach (var pair in Hashes)
            {
                if (!other.Hashes.TryGetValue(pair.Key, out var otherHash)
                    || otherHash != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the appropriate asset bundle target for the current platform.
        /// </summary>
        ///
        /// <remarks>
        /// Uses <see cref="Application.platform"/> to determine the current runtime platform.
        /// </remarks>
        public static AssetBundleTarget CurrentTarget => BundleTargetForPlatform(Application.platform);

        /// <summary>
        /// Returns the appropriate asset bundle target for the specified platform.
        /// </summary>
        public static AssetBundleTarget BundleTargetForPlatform(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return AssetBundleTarget.StandaloneWindows;

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return AssetBundleTarget.StandaloneOSX;

                case RuntimePlatform.Android:
                    return AssetBundleTarget.Android;

                case RuntimePlatform.IPhonePlayer:
                    return AssetBundleTarget.iOS;

                case RuntimePlatform.WebGLPlayer:
                    return AssetBundleTarget.WebGL;

                // TODO: Add support for other platforms.

                default:
                    throw new NotImplementedException($"Cannot determine asset bundle target for unsupported platform {Application.platform}");
            }
        }

        public static bool operator ==(AssetBundleDescription left, AssetBundleDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssetBundleDescription left, AssetBundleDescription right)
        {
            return !left.Equals(right);
        }
    }
}
