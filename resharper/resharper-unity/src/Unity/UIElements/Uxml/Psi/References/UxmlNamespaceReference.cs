using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Resolve;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Resolve;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
  internal class UxmlNamespaceReference : XmlQualifiableReferenceWithToken, IXamlNamespaceReference
  {
    public UxmlNamespaceReference(
      [NotNull] ITreeNode owner, [CanBeNull] IXamlNamespaceReference qualifier,
      [CanBeNull] IXmlToken token, TreeTextRange rangeWithin)
      : base(owner, qualifier, token, rangeWithin)
    {
    }
    
    
    public override ITypeElement GetQualifierTypeElement()
    {
      // var qualifier = GetQualifier();
      //
      // var reference = qualifier as IReference;
      // if (reference == null)
      // {
      //   var expression = qualifier as QualifierExpression;
      //   if (expression != null)
      //     reference = expression.PropertyReference;
      // }
      //
      // return XamlResolveUtil.GetQualifierTypeElement(reference);
      return null;
    }

    protected override bool AllowedNotResolved
    {
      get
      {
        return XamlResolveUtil.IsReferenceInXmlData(this)
               || MarkupCompatibilityUtil.IsIgnorableReference(this);
      }
    }

    public ISymbolTable GetSymbolTable(SymbolTableMode mode)
    {
      return NamespaceReferenceUtil.GetSymbolTable(this);
    }

    public override ResolveResultWithInfo Resolve(ISymbolTable symbolTable, IAccessContext context)
    {
      var resolveResult = base.Resolve(symbolTable, context);
      var namespaceAlias = myOwner as INamespaceAlias;

      if (resolveResult.Info.ResolveErrorType != ResolveErrorType.OK && namespaceAlias?.DeclaredElement.IsUrnAlias == true)
        return ResolveResultWithInfo.Ignore;

      if (resolveResult.Info.ResolveErrorType == ResolveErrorType.NOT_RESOLVED && GetTreeNode().IsWinUINode())
        return ResolveResultWithInfo.Ignore;

      return resolveResult;
    }

    public QualifierKind GetKind() { return QualifierKind.NAMESPACE; }

    public bool Resolved => Resolve().DeclaredElement != null;

    public override Staticness GetStaticness() { return Staticness.OnlyStatic; }

    protected override IReference BindToInternal(IDeclaredElement declaredElement, ISubstitution substitution)
    {
      return NamespaceReferenceUtil.BindTo(this, (INamespace) declaredElement);
    }

    public IXamlNamespaceReference BindModuleTo(IPsiModule module)
    {
      // not sure if it should be invoked
      return this;
    }

    protected override ISymbolFilter[] GetSmartSymbolFilters()
    {
      return new ISymbolFilter[] { XmlResolveFilters.IsNamespace };
    }

    protected override ISymbolFilter[] GetCompletionFilters()
    {
      var language = ReferenceUtil.GetProjectLanguage(myOwner);
      return new ISymbolFilter[]
      {
        new ValidNamesFilter(language),
        XmlResolveFilters.IsNamespace,
        new NoEmptyNamespaceFilter(this, true)
      };
    }
  }
}