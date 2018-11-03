using System;
using System.Linq;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    public static class UnityYamlFileExtensions
    {
        // TODO: What else?
        public static string MetaFileExtensionWithDot = ".meta";
        public static string[] AssetFileExtensionsWithDot = {".unity", ".asset", ".prefab"};
        public static string[] AllFileExtensionsWithDot;
        public static string[] AssetWildCards;

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
    }
}