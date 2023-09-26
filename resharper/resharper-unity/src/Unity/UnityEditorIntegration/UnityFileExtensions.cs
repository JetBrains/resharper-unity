using System;
using JetBrains.Annotations;
// using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.ProjectModel;
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
        public const string AsmDefMetaFileExtensionWithDot = ".asmdef.meta";
        public const string AsmRefFileExtensionWithDot = ".asmref";
        public const string AsmRefMetaFileExtensionWithDot = ".asmref.meta";

        // Game assets - all YAML
        public const string AssetFileExtensionWithDot = ".asset";
        public const string PrefabFileExtensionWithDot = ".prefab";
        public const string SceneFileExtensionWithDot = ".unity";
        public const string ControllerFileExtensionWithDot = ".controller";
        public const string AnimFileExtensionWithDot = ".anim";
        
        public const string InputActionsExtensionWithDot = InputActions.ProjectModel.InputActionsProjectFileType.INPUTACTIONS_EXTENSION;
        public const string UxmlExtensionWithDot = ".uxml"; //UxmlProjectFileType.UXML_EXTENSION;
        
        public const string ResourcesFolderName = "Resources";
        public const string EditorFolderName = "Editor";

        // Data files - does not include .meta
        public static readonly string[] YamlDataFileExtensionsWithDot =
        {
            SceneFileExtensionWithDot,
            AssetFileExtensionWithDot,
            PrefabFileExtensionWithDot,
            ControllerFileExtensionWithDot,
            AnimFileExtensionWithDot
        };

        public static bool IsFromResourceFolder(this IPath path) =>
            path.Components.Any(t => t.Equals(ResourcesFolderName));

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
        
        public static bool IsAsmDefMeta(this IPath path) =>
            SimplePathEndsWith(path, AsmDefMetaFileExtensionWithDot);

        public static bool IsAsmRefMeta(this IPath path) =>
            SimplePathEndsWith(path, AsmRefMetaFileExtensionWithDot);

        
        public static bool IsAsset(this IPath path) =>
            SimplePathEndsWith(path, AssetFileExtensionWithDot);

        public static bool IsAsset(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, AssetFileExtensionWithDot);

        public static bool IsPrefab(this IPath path) =>
            SimplePathEndsWith(path, PrefabFileExtensionWithDot);
        
        public static bool IsPrefab(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, PrefabFileExtensionWithDot);

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
        
        public static bool IsInputActions(this IPath path) =>
            SimplePathEndsWith(path, InputActionsExtensionWithDot);
        
        public static bool IsInputActions(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, InputActionsExtensionWithDot);
        
        public static bool IsUxml(this IPath path) =>
            SimplePathEndsWith(path, UxmlExtensionWithDot);
        
        public static bool IsUxml(this IPsiSourceFile sourceFile) =>
            SourceFileNameEndsWith(sourceFile, UxmlExtensionWithDot);

        public static bool IsYamlDataFile(this IPath path)
        {
            foreach (var extension in YamlDataFileExtensionsWithDot)
            {
                if (SimplePathEndsWith(path, extension))
                    return true;
            }

            return false;
        }

        public static bool IsYamlDataFile(this IPsiSourceFile sourceFile)
        {
            foreach (var extension in YamlDataFileExtensionsWithDot)
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