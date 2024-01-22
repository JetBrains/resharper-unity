using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Impl.Shared;
using JetBrains.ReSharper.Psi.Impl.Shared.References;
using JetBrains.ReSharper.Psi.Impl.Shared.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.References.Members;

// without usage for $
public class OdinRegularMemberReference : OdinMemberReference, ICompletableReference
{
    public OdinRegularMemberReference(ITypeElement targetType, [NotNull] ICSharpLiteralExpression owner, string name, int startOffset, int endOffset) : base(targetType, owner, name, startOffset, endOffset)
    {
    }
}