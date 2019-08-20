using System;
using Newtonsoft.Json;
using UnityEngine;

namespace SynapseGames.AssetBundle
{
    public class Hash128Converter : JsonConverter
    {
        public override bool CanConvert(Type type)
        {
            return type == typeof(Hash128);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var hashString = (string)reader.Value;
            return Hash128.Parse(hashString);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var hash = (Hash128)value;
            var hashString = hash.ToString();
            writer.WriteValue(hashString);
        }
    }
}
