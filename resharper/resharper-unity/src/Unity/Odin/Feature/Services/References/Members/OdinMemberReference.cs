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

public class OdinMemberReference : CheckedReferenceBase<ICSharpLiteralExpression>, IReferenceWithinElement<ITokenNode>, 
    IUnityReferenceFromStringLiteral
{
    private readonly ITypeElement myTargetType;
    private string myName;
    private readonly IAccessContext myAccessContext;

    public OdinMemberReference(ITypeElement targetType, [NotNull] ICSharpLiteralExpression owner, string name, int startOffset, int endOffset) : base(owner)
    {
        myTargetType = targetType;
        myName = name;
        myAccessContext = new DefaultAccessContext(owner);
        ElementRange = new ElementRange<ITokenNode>(owner.Literal,
            new TreeTextRange(new TreeOffset(startOffset), new TreeOffset(endOffset)));
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        var resolveResultWithInfo = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
        return !resolveResultWithInfo.Result.IsEmpty ? resolveResultWithInfo : ResolveResultWithInfo.Unresolved;

    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        if (!myTargetType.IsValid())
            return EmptySymbolTable.INSTANCE;

        var symbolTable = ResolveUtil
            .GetSymbolTableByTypeElement(myTargetType, SymbolTableMode.FULL, myTargetType.Module);
        
        if (useReferenceName)
        {
            var name = GetName();
            return symbolTable.Filter(name, new ExactNameFilter(name));
        }
        return symbolTable;
    }
    
    public override string GetName() => myName;

    public override TreeTextRange GetTreeTextRange()
    {
        if (!RangeWithin.IsValid() || Token == null)
            return TreeTextRange.InvalidRange;

        return RangeWithin.Shift(Token.GetTreeStartOffset());
    }

    public override IReference BindTo(IDeclaredElement element)
    {
        var newName = element.ShortName;
        if (myName.Equals(newName))
            return this;

        var newReference = (ReferenceWithinElementUtil<ITokenNode>.SetText(this, element.ShortName,
            (node, buffer) =>
            {
                // The new name is substituted into the existing text, which includes quotes
                var unquotedStringValue = buffer.GetText(TextRange.FromLength(1, buffer.Length - 2));
                return CSharpElementFactory.GetInstance(node)
                    .CreateStringLiteralExpression(unquotedStringValue)
                    .Literal;
            }) as OdinMemberReference).NotNull();

        newReference.myName = element.ShortName;
        return newReference;
    }

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
        return BindTo(element);
    }

    public override IAccessContext GetAccessContext()
    {
        return myAccessContext;
    }

    public ISymbolTable GetCompletionSymbolTable()
    {
        return GetReferenceSymbolTable(false);
    }

    public override ISymbolFilter[] GetSymbolFilters()
    {
        return Array.Empty<ISymbolFilter>();
    }

    public ITreeNode Token => ElementRange.Token;
    public TreeTextRange RangeWithin => ElementRange.RangeWithin;
    public ElementRange<ITokenNode> ElementRange { get; set; }
}