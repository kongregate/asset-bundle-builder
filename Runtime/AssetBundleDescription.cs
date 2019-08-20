using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SynapseGames.AssetBundle
{
    public class AssetBundleDescription : IEquatable<AssetBundleDescription>
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("hashes")]
        public Dictionary<AssetBundleTarget, Hash128> Hashes = new Dictionary<AssetBundleTarget, Hash128>();

        public string EmbeddedPath => Path.Combine(Application.streamingAssetsPath, Name);

        public AssetBundleDescription() { }

        public AssetBundleDescription(string name)
        {
            Name = name;
        }

        public AssetBundleDescription(string name, Dictionary<AssetBundleTarget, Hash128> hashes)
        {
            Name = name;
            Hashes = hashes;
        }

        public string FileNameForTarget(AssetBundleTarget target)
        {
            if (Hashes.TryGetValue(target, out Hash128 hash))
            {
                return $"{Name}_{target}_{hash}";
            }

            return null;
        }

        public override string ToString()
        {
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
                Hash128 otherHash;
                if (!other.Hashes.TryGetValue(pair.Key, out otherHash)) return false;
                if (otherHash != pair.Value) return false;
            }

            return true;
        }

        public static AssetBundleTarget CurrentTarget
        {
            get
            {
                switch (Application.platform)
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

                    default:
                        throw new NotImplementedException($"Cannot determine asset bundle target for unsupported platform {Application.platform}");
                }
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
