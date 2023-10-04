using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xml.Impl.Resolve;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
  internal class UxmlRootNamespaceReference : XamlReferenceWithTokenBase<NamespaceAliasAttribute>, IXamlNamespaceReference
  {
    public UxmlRootNamespaceReference(
      [NotNull] NamespaceAliasAttribute owner, IXmlToken token, TreeTextRange rangeWithin)
      : base(owner, token, rangeWithin) {  }

    public ISymbolTable GetSymbolTable(SymbolTableMode mode)
    {
       return NamespaceReferenceUtil.GetSymbolTable(this);
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      var resolveResult = base.ResolveWithoutCache();

      if (resolveResult.Info.ResolveErrorType != ResolveErrorType.OK && myOwner.DeclaredElement.IsUrnAlias)
        return ResolveResultWithInfo.Ignore;

      if (resolveResult.Info.ResolveErrorType == ResolveErrorType.NOT_RESOLVED && GetTreeNode().IsWinUINode())
        return ResolveResultWithInfo.Ignore;

      return NamespaceReferenceUtil.CheckModuleResolve(resolveResult, myOwner);
    }

    public QualifierKind GetKind() { return QualifierKind.NAMESPACE; }

    public bool Resolved
    {
      get { return Resolve().DeclaredElement != null; }
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
      return GetNamespaceSymbolTable(GetPsiModule(), withReferences: true);
    }

    public override ISymbolTable GetCompletionSymbolTable()
    {
      return GetNamespaceSymbolTable(GetPsiModule(), withReferences: true).Filter(GetCompletionFilters());
    }

    [NotNull]
    private ISymbolTable GetNamespaceSymbolTable(IPsiModule module, bool withReferences)
    {
      var symbolCache = myOwner.GetPsiServices().Symbols;
      var symbolScope = symbolCache.GetSymbolScope(module, withReferences, caseSensitive: true);
      var globalNamespace = symbolScope.GlobalNamespace;

      var symbolTable = new NamespaceSymbolTable(globalNamespace, module, withReferences, level: 1);
      return symbolTable.Merge(ResolveUtil.CreateSymbolTable(globalNamespace, 1));
    }

    protected override IReference BindToInternal(IDeclaredElement element, ISubstitution substitution)
    {
      return NamespaceReferenceUtil.BindTo(this, (INamespace) element);
    }

    public IXamlNamespaceReference BindModuleTo(IPsiModule module)
    {
      // not sure if it should be invoked
      return this;
    }

    protected override ISymbolFilter[] GetSmartSymbolFilters(out bool applyAllFilters)
    {
      applyAllFilters = false;
      return new ISymbolFilter[] { XmlResolveFilters.IsNamespace };
    }

    protected override ISymbolFilter[] GetCompletionFilters()
    {
      return new ISymbolFilter[]
      {
        XmlResolveFilters.IsNamespace,
        XmlResolveFilters.IsNotRootNamespace,
        new NoEmptyNamespaceFilter(this, true)
      };
    }
  }
}