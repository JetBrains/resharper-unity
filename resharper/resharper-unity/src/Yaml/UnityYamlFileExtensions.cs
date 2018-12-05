using System;
using JetBrains.Annotations;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    public static class UnityYamlFileExtensions
    {
        public const string MetaFileExtensionWithDot = ".meta";
        public static readonly string[] AssetFileExtensionsWithDot = {".unity", ".asset", ".prefab"};
        public static readonly string[] AllFileExtensionsWithDot;

        static UnityYamlFileExtensions()
        {
            AllFileExtensionsWithDot = new string[AssetFileExtensionsWithDot.Length + 1];
            AllFileExtensionsWithDot[0] = MetaFileExtensionWithDot;
            Array.Copy(AssetFileExtensionsWithDot, 0, AllFileExtensionsWithDot, 1, AssetFileExtensionsWithDot.Length);
        }

        public static bool Contains(string extensionWithDot)
        {
            return AllFileExtensionsWithDot.Contains(extensionWithDot, StringComparer.InvariantCultureIgnoreCase);
        }

        public static bool IsAsset([NotNull] this FileSystemPath path)
        {
            return Contains(path.ExtensionWithDot);
        }

        public static bool IsMeta([NotNull] this FileSystemPath path)
        {
            return path.FullPath.EndsWith(MetaFileExtensionWithDot, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}