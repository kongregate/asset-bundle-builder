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
    /// bundle for any supported platform. The values are generated at build time using
    /// <see cref="AssetBundleBuilder"/>
    /// </remarks>
    public class AssetBundleDescription : IEquatable<AssetBundleDescription>
    {
        [JsonProperty("hashes")]
        private Dictionary<AssetBundleTarget, Hash128> _hashes = new Dictionary<AssetBundleTarget, Hash128>();

        /// <summary>
        /// The name of the asset bundle, as specified in the Unity editor.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("dependencies")]
        public HashSet<string> Dependencies { get; }

        /// <summary>
        /// The file name for the current platform, if any. Will be null if there is no
        /// hash for the current platform.
        /// </summary>
        [JsonIgnore]
        public string FileNameForCurrentTarget => GetFileNameForTarget(CurrentTarget);

        /// <summary>
        /// The asset hash for the current platform, if any. Will be null if there is
        /// no hash for the current platform.
        /// </summary>
        [JsonIgnore]
        public Hash128? HashForCurrentTarget => GetHashForTarget(CurrentTarget);

        /// <summary>
        /// The path to the asset bundle when it is distributed as an embedded bundle.
        /// </summary>
        ///
        /// <remarks>
        /// This path will be inside the streaming assets path, which may require
        /// platform-specific considerations when loading. This also does not guarantee
        /// that the bundle is actually embedded in the player, it only provides the
        /// path where the file would be stored if it were embedded.
        /// </remarks>
        [JsonIgnore]
        public string EmbeddedPath => Path.Combine(
            Application.streamingAssetsPath,
            "EmbeddedAssetBundles",
            Name);

        /// <summary>
        /// Cache key to use when downloading the asset bundle for the current platform.
        /// Will be null if there is no asset hash for the current platform.
        /// </summary>
        ///
        /// <remarks>
        /// Used with <see cref="UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle"/>.
        /// </remarks>
        [JsonIgnore]
        public CachedAssetBundle? CachedAssetBundleForCurrentTarget => GetCachedAssetBundleForTarget(CurrentTarget);

        /// <summary>
        /// Initializes a new <see cref="AssetBundleDescription"/>.
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the asset bundle, as defined in the Unity project.
        /// </param>
        ///
        /// <param name="hashes">
        /// The set of asset hashes for each supported platform. Only the specified
        /// platforms will be considered supported, such that the bundle cannot be
        /// loaded on any platforms not specified.
        /// </param>
        [JsonConstructor]
        public AssetBundleDescription(
            string name,
            Dictionary<AssetBundleTarget, Hash128> hashes,
            HashSet<string> dependencies)
        {
            Name = name;
            Dependencies = dependencies;
            _hashes = hashes;
        }

        /// <summary>
        /// Initializes a new <see cref="AssetBundleDescription"/>, copying the name
        /// and hases from <paramref name="other"/>.
        /// </summary>
        public AssetBundleDescription(AssetBundleDescription other)
            : this(other.Name, other._hashes, other.Dependencies)
        {
        }

        /// <summary>
        /// Gets the filename for the bundle on the specified target platform, if supported.
        /// </summary>
        ///
        /// <returns>
        /// The file name for the specified target platform, or null if no hash is
        /// present for that platform.
        /// </returns>
        public string GetFileNameForTarget(AssetBundleTarget target)
        {
            if (_hashes.TryGetValue(target, out var hash))
            {
                return $"{Name}_{target}_{hash}";
            }

            return null;
        }

        /// <summary>
        /// Returns the hash for the specified target platform, if any.
        /// </summary>
        ///
        /// <returns>
        /// The asset hash for the specified platform, or null if no hash is present
        /// for that platform.
        /// </returns>
        public Hash128? GetHashForTarget(AssetBundleTarget target)
        {
            if (_hashes.TryGetValue(target, out var hash))
            {
                return hash;
            }

            return null;
        }

        /// <summary>
        /// Cache key to use when downloading the asset bundle for the specified platform.
        /// Will be null if there is no asset hash for the specified platform.
        /// </summary>
        ///
        /// <remarks>
        /// Used with <see cref="UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle"/>.
        /// </remarks>
        public CachedAssetBundle? GetCachedAssetBundleForTarget(AssetBundleTarget target)
        {
            if (_hashes.TryGetValue(target, out var hash))
            {
                return new CachedAssetBundle(Name, hash);
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
                if (_hashes.TryGetValue(variant, out platformHash))
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

            if (_hashes == other._hashes)
            {
                return true;
            }

            if (_hashes.Count != other._hashes.Count)
            {
                return false;
            }

            foreach (var pair in _hashes)
            {
                if (!other._hashes.TryGetValue(pair.Key, out var otherHash)
                    || otherHash != pair.Value)
                {
                    return false;
                }
            }

            return Dependencies.SetEquals(other.Dependencies);
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
