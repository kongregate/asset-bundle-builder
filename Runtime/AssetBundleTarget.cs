namespace SynapseGames.AssetBundle
{
    /// <summary>
    /// A version of the BuildTarget enum that can be used at runtime. See for
    /// explanations of each variant's meaning <see cref="UnityEditor.BuildTarget"/>.
    /// </summary>
    ///
    /// <remarks>
    /// The purpose of this target is to identify which target platform an asset bundle
    /// can be used on. When building bundles, the <see cref="UnityEditor.BuildTarget"/>
    /// enum is used, however that enum is not available at runtime. This enum allows us
    /// to use the same platform identifiers at built time and runtime.
    /// </remarks>
    public enum AssetBundleTarget
    {
        StandaloneOSX,
        StandaloneWindows,
        StandaloneLinux,
        iOS,
        Android,
        WebGL,
    }
}
