namespace SynapseGames.AssetBundle
{
    /// <summary>
    /// The platform targeted by an asset bundle.
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
    /// This enum is also different from <see cref="UnityEngine.RuntimePlatform"/> in
    /// that it doesn't differentiate between the editor and standalone players, since
    /// the editor uses the same asset bundles as the standalone player for that platform.
    /// </para>
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
