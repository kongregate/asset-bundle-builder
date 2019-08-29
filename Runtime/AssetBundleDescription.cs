using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public struct AssetBundleDescription : IEquatable<AssetBundleDescription>
    {
        private Dictionary<RuntimePlatform, Hash128> _hashes;
        private HashSet<string> _dependencies;

        /// <summary>
        /// The name of the asset bundle, as specified in the Unity editor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The set of supported platforms and their corresponding asset hashes.
        /// </summary>
        public IReadOnlyDictionary<RuntimePlatform, Hash128> Hashes => _hashes;

        public IReadOnlyCollection<string> Dependencies => _dependencies;

        /// <summary>
        /// The file name for the current platform, if any. Will be null if there is no
        /// hash for the current platform.
        /// </summary>
        public string FileNameForCurrentTarget => GetFileNameForPlatform(NormalizedPlatform);

        /// <summary>
        /// The asset hash for the current platform, if any. Will be null if there is
        /// no hash for the current platform.
        /// </summary>
        public Hash128? HashForCurrentTarget => GetHashForPlatform(NormalizedPlatform);

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
        public string EmbeddedPath => Path.Combine(
            Application.streamingAssetsPath,
            "EmbeddedAssetBundles",
            Name);

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
        public AssetBundleDescription(
            string name,
            Dictionary<RuntimePlatform, Hash128> hashes,
            HashSet<string> dependencies)
        {
            Name = name;
            _dependencies = dependencies;
            _hashes = hashes;
        }

        /// <summary>
        /// Initializes a new <see cref="AssetBundleDescription"/>, copying the name
        /// and hashes from <paramref name="other"/>.
        /// </summary>
        public AssetBundleDescription(AssetBundleDescription other)
            : this(other.Name, other._hashes, other._dependencies)
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
        ///
        /// <remarks>
        /// Automatically normalizes <paramref name="platform"/> with <see cref="NormalizePlatform(RuntimePlatform)"/>.
        /// </remarks>
        public string GetFileNameForPlatform(RuntimePlatform platform)
        {
            platform = NormalizePlatform(platform);
            if (Hashes.TryGetValue(platform, out var hash))
            {
                return $"{Name}_{platform}_{hash}";
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
        ///
        /// <remarks>
        /// Automatically normalizes <paramref name="platform"/> with <see cref="NormalizePlatform(RuntimePlatform)"/>.
        /// </remarks>
        public Hash128? GetHashForPlatform(RuntimePlatform platform)
        {
            platform = NormalizePlatform(platform);
            if (Hashes.TryGetValue(platform, out var hash))
            {
                return hash;
            }

            return null;
        }

        public override string ToString()
        {
            // TODO: Include more information in the string representation of the bundle.
            return $"{{ Name = {Name} }}";
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

            // NOTE: We iterate over the variants of RuntimePlatform, rather than
            // directly iterating over the contents of Hashes, in order to hash the
            // values in the dictionary in a deterministic order.
            var variants = Enum.GetValues(typeof(RuntimePlatform)).Cast<RuntimePlatform>();
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

            return _dependencies.SetEquals(other.Dependencies);
        }

        /// <summary>
        /// Returns the appropriate asset bundle target for the current platform.
        /// </summary>
        ///
        /// <remarks>
        /// Uses <see cref="Application.platform"/> to determine the current runtime platform.
        /// </remarks>
        public static RuntimePlatform NormalizedPlatform => NormalizePlatform(Application.platform);

        /// <summary>
        /// Returns the appropriate asset bundle target for the specified platform.
        /// </summary>
        ///
        /// <remarks>
        /// For editor platforms, this will return corresponding platform's player
        /// since the editor does not use different asset bundles from the
        /// corresponding platform.
        /// </remarks>
        public static RuntimePlatform NormalizePlatform(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return RuntimePlatform.WindowsPlayer;

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return RuntimePlatform.OSXPlayer;

                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                    return RuntimePlatform.LinuxPlayer;

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
