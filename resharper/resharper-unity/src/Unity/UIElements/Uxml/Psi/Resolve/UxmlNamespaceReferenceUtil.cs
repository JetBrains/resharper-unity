using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Resolve
{
  internal class UxmlNamespaceReferenceUtil
  {
    [NotNull, Pure]
    public static ISymbolTable GetSymbolTable([NotNull] IUxmlNamespaceReference reference)
    {
      var aliasAttribute = reference.GetTreeNode();

      var symbolCache = aliasAttribute.GetPsiServices().Symbols;
      var symbolScope = symbolCache.GetSymbolScope(aliasAttribute.GetPsiModule(), withReferences: true, caseSensitive: true);

      var nameSpace = reference.Resolve().DeclaredElement as INamespace;
      if (nameSpace == null) return EmptySymbolTable.INSTANCE;

      var nestedElements = nameSpace.GetNestedElements(symbolScope);
      var symbolTable = ResolveUtil.CreateSymbolTable(nestedElements, level: 1);

      return symbolTable;
    }

    public static IReference BindTo([NotNull] IUxmlNamespaceReference reference, [NotNull] INamespace @namespace)
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

        r = (IUxmlNamespaceReference)qualifiableReference.GetQualifier();
      }

      var oldRange = new TreeTextRange(start, reference.RangeWithin.EndOffset);
      var namespaceAlias = reference.GetTreeNode();

      ReferenceWithTokenUtil.SetText(
        (IXmlToken)reference.Token, oldRange, @namespace.QualifiedName, namespaceAlias);

      var end = start + @namespace.QualifiedName.Length - 1;
      foreach (var newReference in namespaceAlias.GetReferences<IUxmlNamespaceReference>())
      {
        var range = newReference.RangeWithin;
        if (range.Contains(end) ||
            (range.Length == 0 && range.StartOffset == start)) return newReference;
      }

      Assertion.Fail("New reference must exist");
      return null;
    }
  }
}