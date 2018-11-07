using System;
using System.Linq;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    public static class UnityYamlFileExtensions
    {
        public const string MetaFileExtensionWithDot = ".meta";
        public static readonly string[] AssetFileExtensionsWithDot = {".unity", ".asset", ".prefab"};
        public static readonly string[] AllFileExtensionsWithDot;
        public static readonly string[] AssetWildCards;

        static UnityYamlFileExtensions()
        {
            AllFileExtensionsWithDot = new string[AssetFileExtensionsWithDot.Length + 1];
            AllFileExtensionsWithDot[0] = MetaFileExtensionWithDot;
            Array.Copy(AssetFileExtensionsWithDot, 0, AllFileExtensionsWithDot, 1, AssetFileExtensionsWithDot.Length);

            AssetWildCards = AssetFileExtensionsWithDot.Select(e => "*" + e).ToArray();
        }

        public static bool Contains(string extensionWithDot)
        {
            return AllFileExtensionsWithDot.Contains(extensionWithDot, StringComparer.InvariantCultureIgnoreCase);
        }

        public static bool IsAsset(FileSystemPath path)
        {
            return AssetFileExtensionsWithDot.Contains(path.ExtensionWithDot, StringComparer.InvariantCultureIgnoreCase);
        }

        public static bool IsMeta(FileSystemPath path)
        {
            return string.Equals(path.ExtensionWithDot, MetaFileExtensionWithDot,
                StringComparison.InvariantCultureIgnoreCase);
        }
    }
}