using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp
{
    public static class ExpressionReferenceUtils
    {
        private static readonly string[] ourInputAxisNames = {"GetAxis", "GetAxisRaw"};
        private static readonly string[] ourInputButtonNames = {"GetButtonDown", "GetButtonUp", "GetButton"};

        public static bool IsTagProperty([CanBeNull] this IReferenceExpression expression)
        {
            if (expression?.NameIdentifier?.Name == "tag")
            {
                var info = expression.Reference.Resolve();
                if (info.ResolveErrorType == ResolveErrorType.OK)
                {
                    var property = info.DeclaredElement as IProperty;
                    var containingType = property?.GetContainingType();
                    if (containingType != null)
                    {
                        var qualifierTypeName = containingType.GetClrName();
                        return KnownTypes.Component.Equals(qualifierTypeName) ||
                               KnownTypes.GameObject.Equals(qualifierTypeName);
                    }
                }
            }

            return false;
        }

        public static bool IsCompareTagMethod(this IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.Component, "CompareTag")
                   || IsSpecificMethod(expr, KnownTypes.GameObject, "CompareTag");
        }

        public static bool IsInputAxisMethod(this IInvocationExpression invocationExpression)
        {
            return IsSpecificMethod(invocationExpression, KnownTypes.Input, ourInputAxisNames);
        }

        public static bool IsInputButtonMethod(this IInvocationExpression invocationExpression)
        {
            return IsSpecificMethod(invocationExpression, KnownTypes.Input, ourInputButtonNames);
        }

        public static bool IsLayerMaskGetMaskMethod(this IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.LayerMask, "GetMask");
        }

        public static bool IsLayerMaskNameToLayerMethod(this IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.LayerMask, "NameToLayer");
        }

        private static bool IsSpecificMethod(IInvocationExpression invocationExpression, IClrTypeName typeName, params string[] methodNames)
        {
            var declaredElement = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;
            if (declaredElement == null)
                return false;

            if (methodNames.Any(t => t.Equals(declaredElement.ShortName)))
                return declaredElement.GetContainingType()?.GetClrName().Equals(typeName) == true;
            return false;
        }

        public static bool IsSceneManagerSceneRelatedMethod(this IInvocationExpressionReference reference)
        {
            return IsSceneRelatedMethod(reference, IsSceneManagerLoadScene);
        }

        public static bool IsEditorSceneManagerSceneRelatedMethod(this IInvocationExpressionReference reference)
        {
            return IsSceneRelatedMethod(reference, IsEditorSceneManagerLoadScene);
        }

        private static bool IsSceneRelatedMethod(IInvocationExpressionReference reference, Func<IMethod, bool> checker)
        {
            var result = reference.Resolve();
            if (checker(result.DeclaredElement as IMethod))
                return true;

            foreach (var candidate in result.Result.Candidates)
            {
                if (checker(candidate as IMethod))
                    return true;
            }

            return false;
        }

        private static bool IsEditorSceneManagerLoadScene(IMethod method)
        {
            if (method != null && method.ShortName.Equals("OpenScene") &&
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.EditorSceneManager) == true)
            {
                return true;
            }

            return false;
        }

        private static bool IsSceneManagerLoadScene(IMethod method)
        {
            if (method != null && method.ShortName.StartsWith("LoadScene") &&
                method.GetContainingType()?.GetClrName().Equals(KnownTypes.SceneManager) == true)
            {
                return true;
            }

            return false;
        }
    }
}