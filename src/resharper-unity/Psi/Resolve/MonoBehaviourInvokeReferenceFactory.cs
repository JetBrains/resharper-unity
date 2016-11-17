using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Resolve
{
    public class MonoBehaviourInvokeReferenceFactory : IReferenceFactory
    {
        private static readonly IClrTypeName MonoBehaviourTypeName = new ClrTypeName("UnityEngine.MonoBehaviour");

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
                if (invokedMethod != null &&
                    (invokedMethod.ShortName == "Invoke"
                    || invokedMethod.ShortName == "InvokeRepeating"
                    || invokedMethod.ShortName == "CancelInvoke"))
                {
                    var containingType = invokedMethod.GetContainingType();
                    if (containingType != null && Equals(containingType.GetClrName(), MonoBehaviourTypeName))
                    {
                        var targetType = invocationExpression.ExtensionQualifier?.GetExpressionType().ToIType()?.GetTypeElement()
                            ?? literal.GetContainingNode<IMethodDeclaration>()?.DeclaredElement?.GetContainingType();

                        // TODO: Check if currentType is derived from MonoBehaviour?
                        if (targetType != null)
                        {
                            IReference reference = new MonoBehaviourInvokeReference(targetType, literal);

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

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            var literal = element as ILiteralExpression;
            if (literal != null && literal.ConstantValue.IsString())
                return names.Contains((string) literal.ConstantValue.Value);
            return false;
        }
    }
}