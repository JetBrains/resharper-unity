using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
    public static class UnityFileExtensions
    {
        // Metadata
        public const string MetaFileExtensionWithDot = ".meta";
        public const string AsmDefFileExtensionWithDot = ".asmdef";
        public const string AsmRefFileExtensionWithDot = ".asmref";

        // Game assets
        public const string AssetFileExtensionWithDot = ".asset";
        public const string PrefabFileExtensionWithDot = ".prefab";
        public const string SceneFileExtensionWithDot = ".unity";
        public const string ControllerFileExtensionWithDot = ".controller";
        public const string AnimFileExtensionWithDot = ".anim";

        private static readonly string[] ourYamlFileExtensionsWithDot =
        {
            SceneFileExtensionWithDot,
            AssetFileExtensionWithDot,
            PrefabFileExtensionWithDot,
            ControllerFileExtensionWithDot,
            AnimFileExtensionWithDot
        };

        public static readonly string[] YamlFileExtensionsWithDot;

        static UnityFileExtensions()
        {
            YamlFileExtensionsWithDot = new string[ourYamlFileExtensionsWithDot.Length + 1];
            YamlFileExtensionsWithDot[0] = MetaFileExtensionWithDot;
            Array.Copy(ourYamlFileExtensionsWithDot, 0, YamlFileExtensionsWithDot, 1, ourYamlFileExtensionsWithDot.Length);
        }

        public static bool IsMeta([NotNull] this IPath path) =>
            SimplePathEndsWith(path, MetaFileExtensionWithDot);

        public static bool IsMeta([NotNull] this IPsiSourceFile psiSourceFile) =>
            SourceFileNameEndsWith(psiSourceFile, MetaFileExtensionWithDot);

        public static bool IsAsmDef([NotNull] this IPath path) =>
            SimplePathEndsWith(path, AsmDefFileExtensionWithDot);

        public static bool IsAsset([NotNull] this IPath path) =>
            SimplePathEndsWith(path, AssetFileExtensionWithDot);

        public static bool IsAsset([NotNull] this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, AssetFileExtensionWithDot);

        public static bool IsPrefab([NotNull] this IPath path) =>
            SimplePathEndsWith(path, PrefabFileExtensionWithDot);

        public static bool IsScene([NotNull] this IPath path) =>
            SimplePathEndsWith(path, SceneFileExtensionWithDot);

        public static bool IsScene([NotNull] this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, SceneFileExtensionWithDot);

        public static bool IsController([NotNull] this IPath path) =>
            SimplePathEndsWith(path, ControllerFileExtensionWithDot);

        public static bool IsController([NotNull] this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, ControllerFileExtensionWithDot);

        public static bool IsAnim([NotNull] this IPath path) =>
            SimplePathEndsWith(path, AnimFileExtensionWithDot);

        public static bool IsAnim([NotNull] this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, AnimFileExtensionWithDot);

        public static bool IsMetaOrProjectSettings(ISolution solution, VirtualFileSystemPath location)
        {
            var components = location.TryMakeRelativeTo(solution.SolutionDirectory).Components.ToArray();

            if (location.ExtensionNoDot.Equals("meta", StringComparison.InvariantCultureIgnoreCase) || components.Length == 2 &&
                components[0].Equals("ProjectSettings", StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public static bool IsIndexedExternalFile([NotNull] this IPath path)
        {
            // TODO: Add .asmdef + .asmref (coming soon)
            return path.IsIndexedYamlExternalFile();
        }

        public static bool IsIndexedYamlExternalFile([NotNull] this IPath path)
        {
            foreach (var extension in ourYamlFileExtensionsWithDot)
            {
                if (SimplePathEndsWith(path, extension))
                    return true;
            }

            return false;
        }

        public static bool IsIndexedYamlExternalFile([NotNull] this IPsiSourceFile sourceFile)
        {
            foreach (var extension in ourYamlFileExtensionsWithDot)
            {
                if (SourceFileNameEndsWith(sourceFile, extension))
                    return true;
            }

            return false;
        }

        // Not to be confused with FileSystemPathEx.EndsWith, which handles path components. This is a simple text
        // comparison, which can handle extensions without allocating another string
        private static bool SimplePathEndsWith(IPath path, string expected) =>
            path.FullPath.EndsWith(expected, StringComparison.InvariantCultureIgnoreCase);

        private static bool SourceFileNameEndsWith(IPsiSourceFile sourceFile, string expected) =>
            sourceFile.Name.EndsWith(expected, StringComparison.InvariantCultureIgnoreCase);
    }
}