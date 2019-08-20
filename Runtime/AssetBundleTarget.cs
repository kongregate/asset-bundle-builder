namespace SynapseGames.AssetBundle
{
    /// <summary>
    /// A version of the BuildTarget enum that can be used at runtime. See for
    /// explanations of each variant's meaning <see cref="UnityEditor.BuildTarget"/>.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The purpose of this target is to identify which target platform an asset bundle
    /// can be used on. When building bundles, the <see cref="UnityEditor.BuildTarget"/>
    /// enum is used, however that enum is not available at runtime. This enum allows us
    /// to use the same platform identifiers at built time and runtime.
    /// </para>
    ///
    /// <para>
    /// The value of each variant matches the corresponding variant in
    /// <see cref="UnityEditor.BuildTarget"/>, so it is safe to cast directly between
    /// <see cref="AssetBundleTarget"/> and <see cref="UnityEditor.BuildTarget"/>. Note,
    /// though, that this enum does not include all of the older, deprecated variants
    /// in <see cref="UnityEditor.BuildTarget"/>.
    /// </para>
    /// </remarks>
    public enum AssetBundleTarget
    {
        StandaloneOSX = 2,
        StandaloneWindows = 5,
        StandaloneLinux = 17,
        iOS = 9,
        Android = 13,
        WebGL = 20,
        WSAPlayer = 21,
    }
}
