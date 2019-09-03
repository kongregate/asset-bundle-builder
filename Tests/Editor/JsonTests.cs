using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SynapseGames.AssetBundle;
using SynapseGames.AssetBundle.Json;
using UnityEngine;

public class JsonTests
{
    public static readonly string SampleJson = @"[
      {
        ""name"": ""bundle-1"",
        ""hashes"": {
          ""Android"": ""982415458cdaf4e60c420f75fe6c8e8b"",
          ""IPhonePlayer"": ""bd7a32acc8931e77eb1174baff409f21"",
          ""WindowsPlayer"": ""5425682f11f1eeb1b0ac7e4580cd2237"",
          ""OSXPlayer"": ""4a8250d93de151328fe1651096ec8bc1"",
          ""WebGLPlayer"": ""bc341e351d4813630d417d33558a51ed""
        },
        ""dependencies"": []
      },
      {
        ""name"": ""bundle-10"",
        ""hashes"": {
          ""Android"": ""7de37f7cd9eca4b2a6c9216c6cda8d0c"",
          ""IPhonePlayer"": ""e3fe65f3b2b19e4d1550cd7210ca5e0d"",
          ""WindowsPlayer"": ""18a5260b52e33fa6e54ff67e14b6c979"",
          ""OSXPlayer"": ""25296e04f8fac4a875b4f774b7bea1ba"",
          ""WebGLPlayer"": ""7a9b673950467554cad5f25f0ec70da5""
        },
        ""dependencies"": [""bundle-1""]
      }
    ]";

    public static readonly string SampleJsonExtended = @"{
      ""metadata"": ""Some cool stuff over here"",
      ""bundle"": {
        ""name"": ""bundle-1"",
        ""hashes"": {
          ""Android"": ""982415458cdaf4e60c420f75fe6c8e8b"",
          ""IPhonePlayer"": ""bd7a32acc8931e77eb1174baff409f21"",
          ""WindowsPlayer"": ""5425682f11f1eeb1b0ac7e4580cd2237"",
          ""OSXPlayer"": ""4a8250d93de151328fe1651096ec8bc1"",
          ""WebGLPlayer"": ""bc341e351d4813630d417d33558a51ed""
        },
        ""dependencies"": []
      }
    }";

    public static readonly AssetBundleDescription[] SampleDescriptions = new AssetBundleDescription[]
    {
        new AssetBundleDescription(
            "bundle-1",
            new Dictionary<RuntimePlatform, Hash128>()
            {
                { RuntimePlatform.Android, Hash128.Parse("982415458cdaf4e60c420f75fe6c8e8b") },
                { RuntimePlatform.IPhonePlayer, Hash128.Parse("bd7a32acc8931e77eb1174baff409f21") },
                { RuntimePlatform.WindowsPlayer, Hash128.Parse("5425682f11f1eeb1b0ac7e4580cd2237") },
                { RuntimePlatform.OSXPlayer, Hash128.Parse("4a8250d93de151328fe1651096ec8bc1") },
                { RuntimePlatform.WebGLPlayer, Hash128.Parse("bc341e351d4813630d417d33558a51ed") },
            },
            new HashSet<string>()),

        new AssetBundleDescription(
            "bundle-10",
            new Dictionary<RuntimePlatform, Hash128>()
            {
                { RuntimePlatform.Android, Hash128.Parse("7de37f7cd9eca4b2a6c9216c6cda8d0c") },
                { RuntimePlatform.IPhonePlayer, Hash128.Parse("e3fe65f3b2b19e4d1550cd7210ca5e0d") },
                { RuntimePlatform.WindowsPlayer, Hash128.Parse("18a5260b52e33fa6e54ff67e14b6c979") },
                { RuntimePlatform.OSXPlayer, Hash128.Parse("25296e04f8fac4a875b4f774b7bea1ba") },
                { RuntimePlatform.WebGLPlayer, Hash128.Parse("7a9b673950467554cad5f25f0ec70da5") },
            },
            new HashSet<string>() { "bundle-1" }),
    };

    [Test]
    public void TestSerialize()
    {
        var json = JsonConvert.SerializeObject(SampleDescriptions, new AssetBundleDescriptionConverter());

        var data = JsonConvert.DeserializeObject<JObject[]>(json);
        var bundle1Name = (string)data[0]["name"];
        var bundle2AndroidHash = (string)data[1]["hashes"]["Android"];
        var bundle2Dependency = (string)data[1]["dependencies"][0];

        Assert.AreEqual(bundle1Name, "bundle-1");
        Assert.AreEqual(bundle2AndroidHash, "7de37f7cd9eca4b2a6c9216c6cda8d0c");
        Assert.AreEqual(bundle2Dependency, "bundle-1");
    }

    [Test]
    public void TestDeserialize()
    {
        var descriptions = JsonConvert.DeserializeObject<AssetBundleDescription[]>(SampleJson, new AssetBundleDescriptionConverter());
        Assert.AreEqual(descriptions, SampleDescriptions);
    }

    [Test]
    public void TestRoundTrip()
    {
        var json = JsonConvert.SerializeObject(SampleDescriptions, new AssetBundleDescriptionConverter());
        var descriptions = JsonConvert.DeserializeObject<AssetBundleDescription[]>(json, new AssetBundleDescriptionConverter());
        Assert.AreEqual(descriptions, SampleDescriptions);
    }

    [Test]
    public void TestDeserializeExtended()
    {
        var description = JsonConvert.DeserializeObject<ExtendedBundleDescription>(SampleJsonExtended, new AssetBundleDescriptionConverter());

        Assert.AreEqual("bundle-1", description.Bundle.Name);
        Assert.AreEqual("Some cool stuff over here", description.CustomData);
        Assert.AreEqual(Hash128.Parse("982415458cdaf4e60c420f75fe6c8e8b"), description.Bundle.GetHashForPlatform(RuntimePlatform.Android));
    }
}

public struct ExtendedBundleDescription
{
    [JsonProperty("bundle")]
    public AssetBundleDescription Bundle;

    [JsonProperty("metadata")]
    public string CustomData;
}
