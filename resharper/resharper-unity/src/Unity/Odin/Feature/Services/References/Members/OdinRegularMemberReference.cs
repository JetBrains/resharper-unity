using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.References.Members;

// without usage for $
public class OdinRegularMemberReference : OdinMemberReference, ICompletableReference
{
    public OdinRegularMemberReference(ITypeElement targetType, [NotNull] ICSharpLiteralExpression owner, string name, int startOffset, int endOffset) : base(targetType, owner, name, startOffset, endOffset)
    {
    }
}