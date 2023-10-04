using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Resolve
{
  internal class NamespaceReferenceUtil
  {
    [NotNull, Pure]
    public static ISymbolTable GetSymbolTable([NotNull] IXamlNamespaceReference reference)
    {
      var aliasAttribute = reference.GetTreeNode();

      var symbolCache = aliasAttribute.GetPsiServices().Symbols;
      var symbolScope =
        symbolCache.GetSymbolScope(aliasAttribute.GetPsiModule(), withReferences: true, caseSensitive: true);

      var nameSpace = reference.Resolve().DeclaredElement as INamespace;
      if (nameSpace == null) return EmptySymbolTable.INSTANCE;

      var nestedElements = nameSpace.GetNestedElements(symbolScope);
      var symbolTable = ResolveUtil.CreateSymbolTable(nestedElements, level: 1);

      return symbolTable;
    }


    [NotNull, Pure]
    public static ResolveResultWithInfo CheckModuleResolve([NotNull] ResolveResultWithInfo resolveResult,
      [CanBeNull] INamespaceAlias namespaceAlias)
    {
      if (resolveResult.ResolveErrorType != ResolveErrorType.OK) return resolveResult;

      var xamlNamespaceAlias = namespaceAlias?.DeclaredElement;
      if (xamlNamespaceAlias?.IsCLRNamespaceAlias != true) return resolveResult;

      var targetModules = xamlNamespaceAlias.GetTargetModules();
      if (targetModules.Any())
      {
        var nameSpace = resolveResult.DeclaredElement as INamespace;
        if (nameSpace != null)
        {
          foreach (var targetModule in targetModules)
          {
            foreach (var module in ReferencedNamespace.GetModulesTable(nameSpace, namespaceAlias))
            {
              if (module.Equals(targetModule))
                return resolveResult;
            }
          }
        }
      }

      return new ResolveResultWithInfo(EmptyResolveResult.Instance, ResolveErrorType.NOT_RESOLVED);
    }

    public static IReference BindTo([NotNull] IXamlNamespaceReference reference, [NotNull] INamespace @namespace)
    {
      var start = TreeOffset.Zero;
      for (var r = reference; r != null;)
      {
        var qualifiableReference = r as IQualifiableReference;
        if (qualifiableReference == null)
        {
          start = r.RangeWithin.StartOffset;
          break;
        }

        r = (IXamlNamespaceReference)qualifiableReference.GetQualifier();
      }

      var oldRange = new TreeTextRange(start, reference.RangeWithin.EndOffset);
      var namespaceAlias = reference.GetTreeNode();

      ReferenceWithTokenUtil.SetText(
        reference.Token, oldRange, @namespace.QualifiedName, namespaceAlias);

      var end = start + @namespace.QualifiedName.Length - 1;
      foreach (var newReference in namespaceAlias.GetReferences<IXamlNamespaceReference>())
      {
        var range = newReference.RangeWithin;
        if (range.Contains(end) ||
            (range.Length == 0 && range.StartOffset == start)) return newReference;
      }

      Assertion.Fail("New reference must exist");
      return null;
    }

    [CanBeNull]
    public static IXamlNamespaceReference BindModuleTo([NotNull] IXamlNamespaceReference reference,
      NamespaceAliasUtil.AssemblyNameRanges ranges, [NotNull] IPsiModule module)
    {
      var namespaceAlias = (INamespaceAlias)reference.GetTreeNode();

      var attributeValue = namespaceAlias.Value;
      if (attributeValue == null || !ranges.AssemblyName.IsValid())
        return null;

      var moduleRange = ranges.AssemblyName;
      var moduleName = NamespaceAliasUtil.GetXamlModuleName(module, namespaceAlias) ?? string.Empty;

      if (moduleName.IsNotEmpty() && ranges.Prefix.IsEmpty)
      {
        moduleName = $";assembly={moduleName}";
      }

      if (moduleName.IsEmpty() && !ranges.Prefix.IsEmpty)
      {
        moduleRange = moduleRange.ExtendLeft(ranges.Prefix.Length);
      }

      var newToken = ReferenceWithTokenUtil.SetText(
        attributeValue.ValueToken, moduleRange, moduleName, namespaceAlias);

      var newOwner = (INamespaceAlias)newToken.Parent;
      foreach (IXamlNamespaceReference newReference in newOwner.GetReferences())
      {
        if (newReference.RangeWithin == reference.RangeWithin) return newReference;
      }

      Assertion.Fail("New reference must exist");
      return null;
    }
  }
}