using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SynapseGames.AssetBundle;
using UnityEngine;

public class JsonTests
{
    public static readonly string SampleJson = @"[
      {
        ""name"": ""bundle-1"",
        ""hashes"": {
          ""Android"": ""982415458cdaf4e60c420f75fe6c8e8b"",
          ""iOS"": ""bd7a32acc8931e77eb1174baff409f21"",
          ""StandaloneWindows"": ""5425682f11f1eeb1b0ac7e4580cd2237"",
          ""StandaloneOSX"": ""4a8250d93de151328fe1651096ec8bc1"",
          ""WebGL"": ""bc341e351d4813630d417d33558a51ed""
        }
      },
      {
        ""name"": ""bundle-10"",
        ""hashes"": {
          ""Android"": ""7de37f7cd9eca4b2a6c9216c6cda8d0c"",
          ""iOS"": ""e3fe65f3b2b19e4d1550cd7210ca5e0d"",
          ""StandaloneWindows"": ""18a5260b52e33fa6e54ff67e14b6c979"",
          ""StandaloneOSX"": ""25296e04f8fac4a875b4f774b7bea1ba"",
          ""WebGL"": ""7a9b673950467554cad5f25f0ec70da5""
        }
      }
    ]";

    public static readonly AssetBundleDescription[] SampleDescriptions = new AssetBundleDescription[]
    {
        new AssetBundleDescription("bundle-1", new Dictionary<AssetBundleTarget, Hash128>()
        {
            { AssetBundleTarget.Android, Hash128.Parse("982415458cdaf4e60c420f75fe6c8e8b") },
            { AssetBundleTarget.iOS, Hash128.Parse("bd7a32acc8931e77eb1174baff409f21") },
            { AssetBundleTarget.StandaloneWindows, Hash128.Parse("5425682f11f1eeb1b0ac7e4580cd2237") },
            { AssetBundleTarget.StandaloneOSX, Hash128.Parse("4a8250d93de151328fe1651096ec8bc1") },
            { AssetBundleTarget.WebGL, Hash128.Parse("bc341e351d4813630d417d33558a51ed") },
        }),

        new AssetBundleDescription("bundle-10", new Dictionary<AssetBundleTarget, Hash128>()
        {
            { AssetBundleTarget.Android, Hash128.Parse("7de37f7cd9eca4b2a6c9216c6cda8d0c") },
            { AssetBundleTarget.iOS, Hash128.Parse("e3fe65f3b2b19e4d1550cd7210ca5e0d") },
            { AssetBundleTarget.StandaloneWindows, Hash128.Parse("18a5260b52e33fa6e54ff67e14b6c979") },
            { AssetBundleTarget.StandaloneOSX, Hash128.Parse("25296e04f8fac4a875b4f774b7bea1ba") },
            { AssetBundleTarget.WebGL, Hash128.Parse("7a9b673950467554cad5f25f0ec70da5") },
        }),
    };

    [Test]
    public void TestSerialize()
    {
        var json = JsonConvert.SerializeObject(SampleDescriptions, new Hash128Converter());

        var data = JsonConvert.DeserializeObject<JObject[]>(json);
        var bundle1Name = (string)data[0]["name"];
        var bundle2AndroidHash = (string)data[1]["hashes"]["Android"];

        Assert.AreEqual(bundle1Name, "bundle-1");
        Assert.AreEqual(bundle2AndroidHash, "7de37f7cd9eca4b2a6c9216c6cda8d0c");
    }

    [Test]
    public void TestDeserialize()
    {
        var descriptions = JsonConvert.DeserializeObject<AssetBundleDescription[]>(SampleJson, new Hash128Converter());
        Assert.AreEqual(descriptions, SampleDescriptions);
    }

    [Test]
    public void TestRoundTrip()
    {
        var json = JsonConvert.SerializeObject(SampleDescriptions, new Hash128Converter());
        var descriptions = JsonConvert.DeserializeObject<AssetBundleDescription[]>(json, new Hash128Converter());
        Assert.AreEqual(descriptions, SampleDescriptions);
    }
}
