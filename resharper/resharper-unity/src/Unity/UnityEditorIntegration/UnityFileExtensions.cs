using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    [PublicAPI]
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

        public static bool IsMeta(this IPath path) =>
            SimplePathEndsWith(path, MetaFileExtensionWithDot);

        public static bool IsMeta(this IPsiSourceFile psiSourceFile) =>
            SourceFileNameEndsWith(psiSourceFile, MetaFileExtensionWithDot);

        public static bool IsAsmDef(this IPath path) =>
            SimplePathEndsWith(path, AsmDefFileExtensionWithDot);

        public static bool IsAsmDef(this IPsiSourceFile psiSourceFile) =>
            SourceFileNameEndsWith(psiSourceFile, AsmDefFileExtensionWithDot);

        public static bool IsAsmRef(this IPath path) =>
            SimplePathEndsWith(path, AsmRefFileExtensionWithDot);

        public static bool IsAsmRef(this IPsiSourceFile psiSourceFile) =>
            SourceFileNameEndsWith(psiSourceFile, AsmRefFileExtensionWithDot);

        public static bool IsAsset(this IPath path) =>
            SimplePathEndsWith(path, AssetFileExtensionWithDot);

        public static bool IsAsset(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, AssetFileExtensionWithDot);

        public static bool IsPrefab(this IPath path) =>
            SimplePathEndsWith(path, PrefabFileExtensionWithDot);

        public static bool IsScene(this IPath path) =>
            SimplePathEndsWith(path, SceneFileExtensionWithDot);

        public static bool IsScene(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, SceneFileExtensionWithDot);

        public static bool IsController(this IPath path) =>
            SimplePathEndsWith(path, ControllerFileExtensionWithDot);

        public static bool IsController(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, ControllerFileExtensionWithDot);

        public static bool IsAnim(this IPath path) =>
            SimplePathEndsWith(path, AnimFileExtensionWithDot);

        public static bool IsAnim(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, AnimFileExtensionWithDot);

        public static bool IsMetaOrProjectSettings(ISolution solution, VirtualFileSystemPath location)
        {
            var components = location.TryMakeRelativeTo(solution.SolutionDirectory).Components.ToArray();

            if (location.ExtensionNoDot.Equals("meta", StringComparison.InvariantCultureIgnoreCase) || components.Length == 2 &&
                components[0].Equals("ProjectSettings", StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        public static bool IsIndexedExternalFile(this IPath path) =>
            path.IsYamlDataFile() || path.IsMeta() || path.IsAsmDef() || path.IsAsmRef();

        public static bool IsYamlDataFile(this IPath path)
        {
            foreach (var extension in ourYamlDataFileExtensionsWithDot)
            {
                if (SimplePathEndsWith(path, extension))
                    return true;
            }

            return false;
        }

        public static bool IsYamlDataFile(this IPsiSourceFile sourceFile)
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