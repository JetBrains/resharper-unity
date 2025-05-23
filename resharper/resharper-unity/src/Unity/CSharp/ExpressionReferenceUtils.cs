using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

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

        public static bool IsAnyAddComponentMethod(this IInvocationExpression invocationExpression)
        {
            if (invocationExpression.Reference.Resolve().DeclaredElement is not IMethod method)
                return false;

            if (!method.ShortName.Equals("AddComponent"))
                return false;

            if (method.ContainingType != null && method.ContainingType.GetClrName().Equals(KnownTypes.IBaker))
                return true;

            return false;
        }

        public static bool IsBakerGetPrimaryEntityMethod(this IInvocationExpression invocationExpression)
        {
            if (invocationExpression.Reference.Resolve().DeclaredElement is not IMethod method)
                return false;

            if (!method.ShortName.Equals("GetEntity"))
                return false;

            if (method.ContainingType == null || !method.ContainingType.GetClrName().Equals(KnownTypes.IBaker))
                return false;

            var parameters = method.Parameters;
            if (parameters.Count != 1)
                return false;

            var parameter = parameters[0];

            return parameter.Type is IDeclaredType declaredType
                   && declaredType.GetClrName().Equals(KnownTypes.TransformUsageFlags);
        }
        
        public static bool IsProfilerBeginSampleMethod(this IInvocationExpression invocationExpression)
        {
            return IsSpecificMethod(invocationExpression, KnownTypes.Profiler, "BeginSample");
        }
        
        public static bool IsCompareTagMethod(this IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.Component, "CompareTag")
                   || IsSpecificMethod(expr, KnownTypes.GameObject, "CompareTag");
        }
        
        public static bool IsFindObjectByTagMethod(this IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.GameObject, "FindWithTag")
                   || IsSpecificMethod(expr, KnownTypes.GameObject, "FindGameObjectWithTag")
                   || IsSpecificMethod(expr, KnownTypes.GameObject, "FindGameObjectsWithTag");
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

        public static bool IsResourcesLoadMethod(this IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsResourcesLoadMethod();
        }
        
        public static bool IsGlobalMethodCreate(this IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsGlobalKeywordCreateMethod();
        }
        
        public static bool IsLocalKeywordConstructor(this IObjectCreationExpression objectCreationExpression)
        {
            return IsSpecificTypeConstructor(objectCreationExpression, KnownTypes.LocalKeyword);

        }
        
        public static bool IsGlobalKeywordConstructor(this IObjectCreationExpression objectCreationExpression)
        {
            return IsSpecificTypeConstructor(objectCreationExpression, KnownTypes.GlobalKeyword);
        }
        
        public static bool IsSpecificTypeConstructor(this IObjectCreationExpression objectCreationExpression, IClrTypeName typeName)
        {
            var resolveResult = objectCreationExpression.Reference?.Resolve();
            if (resolveResult == null)
                return false;

            var candidates = new LocalList<IDeclaredElement>();
            if (resolveResult.DeclaredElement != null)
            {
                candidates.Add(resolveResult.DeclaredElement);
            }
            else
            {
                candidates.AddRange(resolveResult.Result.Candidates);
            }
            
            foreach (var candidate in candidates)
            {
                var type = (candidate as IConstructor)?.ContainingType;
                if (type == null)
                    continue;

                if (type.GetClrName().Equals(typeName))
                    return true;
            }
            
            return false;
        }
        
        public static bool IsAssetDataBaseLoadMethod(this IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsAssetDataBaseLoadMethod();
        }
        
        public static bool IsIBakerAddComponentMethod(this IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsIBakerAddComponentMethod();
        }
        
        public static bool IsIBakerAddComponentObjectMethod(this IInvocationExpression invocationExpression)
        {
            return invocationExpression.InvocationExpressionReference.IsIBakerAddComponentObjectMethod();
        }

        private static bool IsSpecificMethod(IInvocationExpression invocationExpression, IClrTypeName typeName, params string[] methodNames)
        {
            var declaredElement = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;
            if (declaredElement == null)
                return false;

            if (methodNames.Any(t => t.Equals(declaredElement.ShortName)))
                return declaredElement.ContainingType?.GetClrName().Equals(typeName) == true;
            return false;
        }
        
        internal static string GetInvocationTypeArgumentName(IInvocationExpression invocationExpression)
        {
            var invocationTypeArguments = invocationExpression.Reference.Invocation.TypeArguments;
            return invocationTypeArguments.Count == 1 ? invocationTypeArguments[0].GetScalarType()?.GetClrName().FullName : null;
        }

        public static bool IsSceneManagerSceneRelatedMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsSceneManagerLoadScene);
        }

        public static bool IsEditorSceneManagerSceneRelatedMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsEditorSceneManagerLoadScene);
        }
        
        public static bool IsAnimatorPlayMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsAnimatorPlay);
        }

        public static bool IsResourcesLoadMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsResourcesLoad);
        }
        
        public static bool IsGlobalKeywordCreateMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsGlobalKeywordCreate);
        }

        public static bool IsAssetDataBaseLoadMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsAssetDataBaseLoad);
        }

        public static bool IsIBakerAddComponentMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsBakerAddComponentMethod);
        }
        
        public static bool IsIBakerAddComponentObjectMethod(this IInvocationExpressionReference reference)
        {
            return IsRelatedMethod(reference, IsIBakerAddComponentObjectMethod);
        }

        public static bool IsJobWithCodeMethod(this IInvocationExpression invocationExpression)
        {
            return IsRelatedMethod(invocationExpression.InvocationExpressionReference, IsJobWithCodeMethod);
        }

        public static bool IsEntitiesForEach(this IInvocationExpression invocationExpression)
        {
            return IsRelatedMethod(invocationExpression.InvocationExpressionReference, IsEntitiesForEach);
        }
        private static bool IsEntitiesForEachWithoutBurst(this IInvocationExpression invocationExpression)
        {
            return IsRelatedMethod(invocationExpression.InvocationExpressionReference, IsEntitiesForEachWithoutBurst);
        }
        
        public static bool IsInsideRunWithoutBurstForeach(this ITreeNode treeNode)
        {
            var invocationExpression = treeNode.GetContainingNode<IInvocationExpression>(returnThis: true);
            while (invocationExpression != null)
            {
                if (invocationExpression.IsEntitiesForEachWithoutBurst())
                    return true;

                invocationExpression = invocationExpression.GetContainingNode<IInvocationExpression>();
            }

            return false;
        }
        
        public static bool IsSystemApiQuery(this IInvocationExpression invocationExpression)
        {
            return invocationExpression.TypeArguments.Count != 0 &&
                   IsRelatedMethod(invocationExpression.InvocationExpressionReference, IsSystemApiQuery);
        }
        
        private static bool IsRelatedMethod(IInvocationExpressionReference reference, Func<IMethod, bool> checker)
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
            return method != null 
                   && method.ShortName.Equals("OpenScene") && method.ContainingType?.GetClrName().Equals(KnownTypes.EditorSceneManager) == true;
        }

        private static bool IsSceneManagerLoadScene(IMethod method)
        {
            return method != null 
                   && method.ShortName.StartsWith("LoadScene") 
                   && method.ContainingType?.GetClrName().Equals(KnownTypes.SceneManager) == true;
        }
        
        private static bool IsAnimatorPlay(IMethod method)
        {
            return method != null &&
                   method.ShortName.StartsWith("Play") && method.ContainingType?.GetClrName().Equals(KnownTypes.Animator) == true;
        }

        private static bool IsResourcesLoad(IMethod method)
        {
            return method != null &&
                   method.ShortName.StartsWith("Load") && method.ContainingType?.GetClrName().Equals(KnownTypes.Resources) == true;
        }        
        
        private static bool IsGlobalKeywordCreate(IMethod method)
        {
            return method != null &&
                   method.ShortName.StartsWith("Create") && method.ContainingType?.GetClrName().Equals(KnownTypes.GlobalKeyword) == true;
        }        
        
        private static bool IsAssetDataBaseLoad(IMethod method)
        {
            return method != null &&
                   method.ShortName.StartsWith("Load") && method.ContainingType?.GetClrName().Equals(KnownTypes.AssetDatabase) == true;
        }

        private static bool IsBakerAddComponentMethod(IMethod method)
        {
            return method is { ShortName: "AddComponent" } &&
                   method.ContainingType?.GetClrName().Equals(KnownTypes.IBaker) == true;
        }

        private static bool IsIBakerAddComponentObjectMethod(IMethod method)
        {
            return method is { ShortName: "AddComponentObject" } &&
                   method.ContainingType?.GetClrName().Equals(KnownTypes.IBaker) == true;
        }

        private static bool IsJobWithCodeMethod(IMethod method)
        {
            return method is { ShortName: "WithCode" } &&
                   method.ContainingType?.GetClrName().Equals(KnownTypes.LambdaSingleJobDescriptionConstructionMethods) == true;
        }

        private static bool IsEntitiesForEach(IMethod method)
        {
            return method is { ShortName: "ForEach" } &&
                   method.ContainingType?.GetClrName().Equals(KnownTypes.LambdaForEachDescriptionConstructionMethods) == true;
        }
        
        private static bool IsEntitiesForEachWithoutBurst(IMethod method)
        {
            return method is { ShortName: "WithoutBurst" } &&
                   method.ContainingType?.GetClrName().Equals(KnownTypes.LambdaJobDescriptionConstructionMethods) == true;
        }
        
        private static bool IsSystemApiQuery(IMethod method)
        {
            return method is { ShortName: "Query" } &&
                   method.ContainingType?.GetClrName().Equals(KnownTypes.SystemAPI) == true;
        }
        
        public static bool IsUQueryExtensionsQueueMethod(this IInvocationExpression expr)
        {
            return IsSpecificMethod(expr, KnownTypes.UQueryExtensions, "Q", "Query");
        }
    }
}