using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    public static class UnityFileExtensions
    {
        // Metadata (.meta is YAML, .asmdef/.asmref is JSON)
        public const string MetaFileExtensionWithDot = ".meta";
        public const string AsmDefFileExtensionWithDot = ".asmdef";
        public const string AsmRefFileExtensionWithDot = ".asmref";

        // Game assets - all YAML
        public const string AssetFileExtensionWithDot = ".asset";
        public const string PrefabFileExtensionWithDot = ".prefab";
        public const string SceneFileExtensionWithDot = ".unity";
        public const string ControllerFileExtensionWithDot = ".controller";
        public const string AnimFileExtensionWithDot = ".anim";

        // Data files - does not include .meta
        private static readonly string[] ourYamlDataFileExtensionsWithDot =
        {
            SceneFileExtensionWithDot,
            AssetFileExtensionWithDot,
            PrefabFileExtensionWithDot,
            ControllerFileExtensionWithDot,
            AnimFileExtensionWithDot
        };

        // All YAML files, including .meta
        public static readonly string[] AllYamlFileExtensionsWithDot;

        static UnityFileExtensions()
        {
            AllYamlFileExtensionsWithDot = new string[ourYamlDataFileExtensionsWithDot.Length + 1];
            AllYamlFileExtensionsWithDot[0] = MetaFileExtensionWithDot;
            Array.Copy(ourYamlDataFileExtensionsWithDot, 0, AllYamlFileExtensionsWithDot, 1, ourYamlDataFileExtensionsWithDot.Length);
        }

        public static bool IsMeta([NotNull] this IPath path) =>
            SimplePathEndsWith(path, MetaFileExtensionWithDot);

        public static bool IsMeta([NotNull] this IPsiSourceFile psiSourceFile) =>
            SourceFileNameEndsWith(psiSourceFile, MetaFileExtensionWithDot);

        public static bool IsAsmDef([NotNull] this IPath path) =>
            SimplePathEndsWith(path, AsmDefFileExtensionWithDot);

        public static bool IsAsmDef([NotNull] this IPsiSourceFile psiSourceFile) =>
            SourceFileNameEndsWith(psiSourceFile, AsmDefFileExtensionWithDot);

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
            // TODO: Add .asmref (coming soon)
            return path.IsYamlDataFile() || path.IsMeta() || path.IsAsmDef();
        }

        public static bool IsYamlDataFile([NotNull] this IPath path)
        {
            foreach (var extension in ourYamlDataFileExtensionsWithDot)
            {
                if (SimplePathEndsWith(path, extension))
                    return true;
            }

            return false;
        }

        public static bool IsYamlDataFile([NotNull] this IPsiSourceFile sourceFile)
        {
            foreach (var extension in ourYamlDataFileExtensionsWithDot)
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