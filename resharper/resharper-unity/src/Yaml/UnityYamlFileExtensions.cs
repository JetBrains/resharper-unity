using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    public static class UnityYamlFileExtensions
    {
        public const string MetaFileExtensionWithDot = ".meta";
        public const string AssetFileExtensionWithDot = ".asset";
        public const string PrefabFileExtensionWithDot = ".prefab";
        public const string SceneFileExtensionWithDot = ".unity";
        public const string ControllerFileExtensionWithDot = ".controller";
        public const string AnimFileExtensionWithDot = ".anim";

        public static readonly string[] AssetFileExtensionsWithDot =
        {
            SceneFileExtensionWithDot,
            AssetFileExtensionWithDot,
            PrefabFileExtensionWithDot,
            ControllerFileExtensionWithDot,
            AnimFileExtensionWithDot
        };
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

        public static bool IsAsset([NotNull] this IPath path)
        {
            return SimplePathEndsWith(path, AssetFileExtensionWithDot);
        }

        public static bool IsPrefab([NotNull] this IPath path)
        {
            return SimplePathEndsWith(path, PrefabFileExtensionWithDot);
        }

        public static bool IsScene([NotNull] this IPath path)
        {
            return SimplePathEndsWith(path, SceneFileExtensionWithDot);
        }

        // Do we have a better name than "asset" here? It's confusingly overloaded with .asset
        public static bool IsInterestingAsset([NotNull] this IPath path)
        {
            foreach (var extension in AssetFileExtensionsWithDot)
            {
                if (SimplePathEndsWith(path, extension))
                    return true;
            }

            return false;
        }
        
        public static bool IsControllerFile([NotNull] this IPath path)
        {
            return SimplePathEndsWith(path, ControllerFileExtensionWithDot);
        }
        
        public static bool IsAnimFile([NotNull] this IPath path)
        {
            return SimplePathEndsWith(path, AnimFileExtensionWithDot);
        }

        public static bool IsMeta([NotNull] this IPath path)
        {
            return SimplePathEndsWith(path, MetaFileExtensionWithDot);
        }

        public static bool IsMetaOrProjectSettings(ISolution solution, FileSystemPath location)
        {
            var components = location.TryMakeRelativeTo(solution.SolutionDirectory).Components.ToArray();
            
            if (location.ExtensionNoDot.Equals("meta", StringComparison.InvariantCultureIgnoreCase) || components.Length == 2 &&
                components[0].Equals("ProjectSettings", StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        } 

        // Not to be confused with FileSystemPathEx.EndsWith, which handles path components. This is a simple text
        // comparison, which can handle extensions without allocating another string
        private static bool SimplePathEndsWith(IPath path, string expected)
        {
            return path.FullPath.EndsWith(expected, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}