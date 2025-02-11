using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using AssetRipper.Primitives;
using MonoMod.Utils;

[assembly: InternalsVisibleTo("BepInEx.Unity.Mono.Preloader")]
[assembly: InternalsVisibleTo("BepInEx.Unity.Mono")]
[assembly: InternalsVisibleTo("BepInEx.Unity.IL2CPP")]

namespace BepInEx.Unity.Common;

/// <summary>
///     Various information about the currently executing Unity player.
/// </summary>
public static class UnityInfo
{
    // Adapted from https://github.com/SamboyCoding/Cpp2IL/blob/development/LibCpp2IL/LibCpp2IlMain.cs
    private static readonly ManagerLookup[] ManagerVersionLookup =
    {
        new("globalgamemanagers", 0x14, 0x30),
        new("data.unity3d", 0x12),
        new("mainData", 0x14)
    };

    private static bool initialized;

    /// <summary>
    ///     Path to the player executable.
    /// </summary>
    public static string PlayerPath { get; private set; }

    /// <summary>
    ///     Path to the game data directory (directory that contains the game assets).
    /// </summary>
    public static string GameDataPath { get; private set; }

    /// <summary>
    ///     Version of the Unity player
    /// </summary>
    /// <remarks>
    ///     Because BepInEx can execute very early, the exact Unity version might not be available in early
    ///     bootstrapping phases. The version should be treated as an estimation of the actual version of the Unity player.
    /// </remarks>
    public static UnityVersion Version { get; private set; }

    internal static void Initialize(string unityPlayerPath, string gameDataPath)
    {
        if (initialized)
            return;
        PlayerPath = Path.GetFullPath(unityPlayerPath ?? throw new ArgumentNullException(nameof(unityPlayerPath)));
        GameDataPath = Path.GetFullPath(gameDataPath ?? throw new ArgumentNullException(nameof(gameDataPath)));

        DetermineVersion();
        initialized = true;
    }

    internal static void SetRuntimeUnityVersion(string version) => Version = UnityVersion.Parse(version);

    private static void DetermineVersion()
    {
        Version = new UnityVersion(2019, 4, 40, UnityVersionType.Final);
    }

    private class ManagerLookup
    {
        private readonly string filePath;
        private readonly int[] lookupOffsets;

        public ManagerLookup(string filePath, params int[] lookupOffsets)
        {
            this.filePath = filePath;
            this.lookupOffsets = lookupOffsets;
        }

        public bool TryLookup(out UnityVersion version)
        {
            var path = Path.Combine(GameDataPath, filePath);
            if (!File.Exists(path))
            {
                version = default;
                return false;
            }

            using var fs = File.OpenRead(path);
            foreach (var offset in lookupOffsets)
            {
                var sb = new StringBuilder();
                fs.Position = offset;

                byte b;
                while ((b = (byte) fs.ReadByte()) != 0)
                    sb.Append((char) b);

                try
                {
                    version = UnityVersion.Parse(sb.ToString());
                    return true;
                }
                catch (Exception e)
                {
                    // Ignore
                }
            }

            version = default;
            return false;
        }
    }
}
