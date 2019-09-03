using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SynapseGames.AssetBundle.Json
{
    public class AssetBundleDescriptionConverter : JsonConverter<AssetBundleDescription>
    {
        public override AssetBundleDescription ReadJson(
            JsonReader reader,
            Type objectType,
            AssetBundleDescription existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);

            var name = jsonObject.Value<string>("name");

            var hashes = new Dictionary<RuntimePlatform, Hash128>();
            foreach (var property in jsonObject.Value<JObject>("hashes"))
            {
                if (!Enum.TryParse(property.Key, out RuntimePlatform platform))
                {
                    throw new JsonSerializationException($"Could not parse unknown asset bundle target {property.Key}");
                }

                var hash = Hash128.Parse((string)property.Value);
                if (!hash.isValid)
                {
                    throw new JsonSerializationException($"Invalid hash {property.Value} found in bundle {name} for platform {platform}");
                }

                hashes.Add(platform, hash);
            }

            var dependencies = new HashSet<string>();
            serializer.Populate(jsonObject.Value<JArray>("dependencies").CreateReader(), dependencies);

            return new AssetBundleDescription(name, hashes, dependencies);
        }

        public override void WriteJson(
            JsonWriter writer,
            AssetBundleDescription value,
            JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // Serialize the name of the bundle.
            writer.WritePropertyName("name");
            writer.WriteValue(value.Name);

            // Serialize the set of platforms and hashes.
            writer.WritePropertyName("hashes");
            writer.WriteStartObject();
            foreach (var pair in value.Hashes)
            {
                writer.WritePropertyName(pair.Key.ToString());
                writer.WriteValue(pair.Value.ToString());
            }
            writer.WriteEndObject();

            // Serialize the list of dependencies.
            writer.WritePropertyName("dependencies");
            serializer.Serialize(writer, value.Dependencies);

            writer.WriteEndObject();
        }
    }
}
