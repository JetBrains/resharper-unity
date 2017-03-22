﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public class UnityEventFunctionReferenceFactory : IReferenceFactory
    {
        private static readonly HashSet<string> ReferencingMethodNames = new HashSet<string>
        {
            "Invoke", "InvokeRepeating", "CancelInvoke", "IsInvoking",
            "StartCoroutine", "StopCoroutine"
        };

#if RIDER || WAVE08
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            // If the old references are all attached to this element, 
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<UnityEventFunctionReference>(oldReferences, element))
            {
                return oldReferences;
            }

            var literal = element as ILiteralExpression;
            if (literal == null || !literal.ConstantValue.IsString())
                return ReferenceCollection.Empty;

            if (!IsStringLiteralFirstArgument(literal))
                return ReferenceCollection.Empty;

            var invocationExpression = literal.GetContainingNode<IInvocationExpression>();
            var invocationReference = invocationExpression?.Reference;
            var invokedMethod = invocationReference?.Resolve().DeclaredElement as IMethod;
            if (invokedMethod != null && DoesMethodReferenceFunction(invokedMethod))
            {
                var containingType = invokedMethod.GetContainingType();
                if (containingType != null && Equals(containingType.GetClrName(), KnownTypes.MonoBehaviour))
                {
                    var targetType = invocationExpression.ExtensionQualifier?.GetExpressionType()
                                         .ToIType()?.GetTypeElement()
                                     ??
                                     literal.GetContainingNode<IMethodDeclaration>()?
                                         .DeclaredElement?.GetContainingType();

                    if (targetType != null)
                    {
                        var reference = new UnityEventFunctionReference(targetType, literal);
                        return new ReferenceCollection(reference);
                    }
                }
            }

            return ReferenceCollection.Empty;
        }

        private static bool IsStringLiteralFirstArgument(ILiteralExpression literal)
        {
            var argument = CSharpArgumentNavigator.GetByValue(literal as ICSharpExpression);
            var argumentsOwner = CSharpArgumentsOwnerNavigator.GetByArgument(argument);
            return argumentsOwner != null && argumentsOwner.ArgumentsEnumerable.FirstOrDefault() == argument;
        }

#else

        public IReference[] GetReferences(ITreeNode element, IReference[] oldReferences)
        {
            var literal = element as ILiteralExpression;
            if (literal != null && literal.ConstantValue.IsString())
            {
                var argument = CSharpArgumentNavigator.GetByValue(literal as ICSharpExpression);
                var argumentsOwner = CSharpArgumentsOwnerNavigator.GetByArgument(argument);
                if (argumentsOwner != null && argumentsOwner.ArgumentsEnumerable.FirstOrDefault() != argument)
                {
                    return EmptyArray<IReference>.Instance;
                }

                var invocationExpression = literal.GetContainingNode<IInvocationExpression>();
                var invocationReference = invocationExpression?.Reference;
                var invokedMethod = invocationReference?.Resolve().DeclaredElement as IMethod;
                if (invokedMethod != null && DoesMethodReferenceFunction(invokedMethod))
                {
                    var containingType = invokedMethod.GetContainingType();
                    if (containingType != null && Equals(containingType.GetClrName(), KnownTypes.MonoBehaviour))
                    {
                        var targetType = invocationExpression.ExtensionQualifier?.GetExpressionType().ToIType()?.GetTypeElement()
                            ?? literal.GetContainingNode<IMethodDeclaration>()?.DeclaredElement?.GetContainingType();

                        // TODO: Check if currentType is derived from MonoBehaviour?
                        if (targetType != null)
                        {
                            IReference reference = new UnityEventFunctionReference(targetType, literal);

                            return oldReferences != null && oldReferences.Length == 1
                                   && Equals(oldReferences[0], reference)
                                ? oldReferences
                                : new[] {reference};
                        }
                    }
                }
            }

            return EmptyArray<IReference>.Instance;
        }
#endif

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            var literal = element as ILiteralExpression;
            if (literal != null && literal.ConstantValue.IsString())
                return names.Contains((string) literal.ConstantValue.Value);
            return false;
        }

        private static bool DoesMethodReferenceFunction(IMethod invokedMethod)
        {
            return ReferencingMethodNames.Contains(invokedMethod.ShortName);
        }
    }
}