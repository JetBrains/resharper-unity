#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve
{
    public class ShaderReferenceFactory : StringLiteralReferenceFactoryBase
    {
        private const string ShaderFindMethodName = "Find";
        private const string ShaderTypeName = "Shader";         
        
        public override ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<IShaderReference>(oldReferences, element))
                return oldReferences;
            
            var literal = GetValidStringLiteralExpression(element);
            if (literal == null)
                return ReferenceCollection.Empty;
            
            if (CSharpResolveUtils.TryGetInvocationByArgumentValue(literal, out var argument) is not {} invocationExpression)
                return ReferenceCollection.Empty;

            if (invocationExpression.ArgumentList.Arguments[0] != argument)
                return ReferenceCollection.Empty;

            if (invocationExpression.TryGetInvokedMethod() is not { ShortName: ShaderFindMethodName, ContainingType: { ShortName: ShaderTypeName } invocationTarget }
                || invocationTarget.GetContainingNamespace().QualifiedName != WellKnownUnityNamespaces.UnityEngine)
                return ReferenceCollection.Empty;
            
            return new ReferenceCollection(new ShaderReference<ICSharpLiteralExpression>(literal, CSharpLiteralReferenceOrigin.Instance));
        }
        
        private class CSharpLiteralReferenceOrigin : IReferenceOrigin<ICSharpLiteralExpression>
        {
            public static readonly CSharpLiteralReferenceOrigin Instance = new();
            
            public string? GetReferenceName(ICSharpLiteralExpression owner) => owner.ConstantValue.StringValue;

            public TreeTextRange GetReferenceNameRange(ICSharpLiteralExpression owner) => owner.GetStringLiteralContentTreeRange() is { Length: > 0 } range ? range : TreeTextRange.InvalidRange;

            public IReference RenameFromReference(IReference fromReference, ICSharpLiteralExpression owner, string newName, ISubstitution? substitution)
            {
                var literalAlterer = StringLiteralAltererUtil.CreateStringLiteralByExpression(owner);
                var stringValue = owner.ConstantValue.StringValue.NotNull();
                literalAlterer.Replace(stringValue, newName);
                var newOwner = literalAlterer.Expression;
                if (!owner.Equals(newOwner))
                    return newOwner.FindReference<IReference>(r => r.GetType() == GetType()) ?? fromReference;
                return fromReference;
            }
        }
    }
}