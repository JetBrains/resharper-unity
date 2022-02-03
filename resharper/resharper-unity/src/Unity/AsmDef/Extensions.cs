using System.Diagnostics.CodeAnalysis;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef
{
    public static class Extensions
    {
        public static bool IsNamePropertyValue([NotNullWhen(true)] this ITreeNode? node) =>
            node.AsStringLiteralValue().IsRootPropertyValue("name");

        public static bool IsReferencePropertyValue([NotNullWhen(true)] this ITreeNode? node) =>
            node.AsStringLiteralValue().IsRootPropertyValue("reference");

        public static bool IsReferencesArrayEntry([NotNullWhen(true)] this ITreeNode? node)
        {
            var value = node.AsStringLiteralValue();
            var array = JsonNewArrayNavigator.GetByValue(value);
            return array.IsRootPropertyValue("references");
        }

        public static bool IsDefineConstraintsArrayEntry([NotNullWhen(true)] this ITreeNode? node)
        {
            var value = node.AsStringLiteralValue();
            var array = JsonNewArrayNavigator.GetByValue(value);
            return array.IsRootPropertyValue("defineConstraints");
        }

        public static bool IsVersionDefinesObjectNameValue([NotNullWhen(true)] this ITreeNode? node) =>
            IsVersionDefinesObjectPropertyValue(node, "name");

        public static bool IsVersionDefinesObjectExpressionValue([NotNullWhen(true)] this ITreeNode? node) =>
            IsVersionDefinesObjectPropertyValue(node, "expression");

        public static bool IsVersionDefinesObjectDefineValue([NotNullWhen(true)] this ITreeNode? node) =>
            IsVersionDefinesObjectPropertyValue(node, "define");

        private static bool IsVersionDefinesObjectPropertyValue([NotNullWhen(true)] this ITreeNode? node, string expectedPropertyKey)
        {
            var value = node.AsStringLiteralValue();
            var defineProperty = value.GetNamedMemberByValue(expectedPropertyKey);
            var versionDefineObject = JsonNewObjectNavigator.GetByMember(defineProperty);
            var versionDefinesArray = JsonNewArrayNavigator.GetByValue(versionDefineObject);
            return versionDefinesArray.IsRootPropertyValue("versionDefines");
        }
    }
}