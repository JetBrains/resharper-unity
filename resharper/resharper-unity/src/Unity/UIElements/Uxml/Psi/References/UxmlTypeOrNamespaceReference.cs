using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Impl.Shared.References;
using JetBrains.ReSharper.Psi.Impl.Shared.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree.References;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    internal class UxmlNsAliasReference : ReferenceWithinElementBase<IXmlTreeNode, IXmlToken>, ICompletableReference, IQualifier, IReferenceWithToken
    {
        public UxmlNsAliasReference(IXmlTreeNode owner, IXmlIdentifier token) : base(owner, token, token.XmlNamespaceRange)
        {
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var psiServices = myOwner.GetPsiServices();
            var symbolTable = EmptySymbolTable.INSTANCE;
            IXmlTag xmlTag = null;
            if (myOwner is IXmlTagHeader header) xmlTag = XmlTagNavigator.GetByTagHeader(header);
            else if (myOwner is IXmlTagFooter footer) xmlTag = XmlTagNavigator.GetByTagFooter(footer);
            
            while (xmlTag != null)
            {
                var table = new SymbolTable(psiServices);
                foreach (var nsAlias in xmlTag.GetAttributes().OfType<UxmlNamespaceAliasAttribute>())
                {
                    table.AddSymbol(nsAlias);
                }
                xmlTag = XmlTagNavigator.GetByTag(xmlTag);
                if(table != EmptySymbolTable.INSTANCE)
                    symbolTable = symbolTable.Merge(table);
            }

            return symbolTable;
        }

        protected override IReference BindToInternal(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (declaredElement is ITypeElement newType)
            {
                var target = Resolve().DeclaredElement;
                if (target is UxmlNamespaceAliasAttribute { Value: XmlValueToken xmlValueToken } attribute)
                {
                    ReferenceWithTokenUtil.SetText(xmlValueToken, xmlValueToken.UnquotedValueRange, newType.GetContainingNamespace().QualifiedName);
                    attribute.ResetReferences();
                }
            }

            return this;
        }

        public ISymbolTable GetSymbolTable(SymbolTableMode mode)
        {
            if (Resolve().DeclaredElement is not UxmlNamespaceAliasAttribute namespaceAliasAttribute)
                return EmptySymbolTable.INSTANCE;
            var references = namespaceAliasAttribute.GetReferences<IUxmlNamespaceReference>();
            return UxmlNamespaceReferenceUtil.GetSymbolTable(references.Last());
        }

        public QualifierKind GetKind() => QualifierKind.NAMESPACE;

        public bool Resolved => Resolve().DeclaredElement != null;
    }
    
    internal class UxmlTypeOrNamespaceReference : QualifiableReferenceWithinElement<IXmlTreeNode,IXmlToken>, 
        IReferenceQualifier, ICompletableReference, IReferenceWithToken
    {
        private readonly ISymbolCache mySymbolCache;
        private readonly ExpectedVisualElementTypeFilter myTypeFilter;

        public UxmlTypeOrNamespaceReference(IXmlTreeNode owner, [CanBeNull] IQualifier qualifier,
            IXmlToken token, TreeTextRange rangeWithin, ISymbolCache symbolCache,
                                                   bool isFinalPart)
            : base(owner, qualifier, token, rangeWithin)
        {
            mySymbolCache = symbolCache;
            myTypeFilter = new ExpectedVisualElementTypeFilter(mustBeClass: isFinalPart);
        }

        public override Staticness GetStaticness() => Staticness.Any;

        // Qualifier is either null or a namespace. No type element
        public override ITypeElement GetQualifierTypeElement() => null;

        protected override IReference BindToInternal(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (string.IsNullOrEmpty(declaredElement.ShortName)) return this;
            
            // Fix up name
            if (declaredElement.ShortName != GetName())
            {
                var newReference = ReferenceWithTokenUtil.SetText(this, declaredElement.ShortName);
                return newReference.BindTo(declaredElement);
            }

            // Fix up qualification (e.g. move namespace)
            if (declaredElement is ITypeElement newType && !newType.Equals(Resolve().DeclaredElement))
            {
                var qualifier = GetQualifier();
                if (qualifier is UxmlNsAliasReference uxmlNsAliasReference)
                {
                    uxmlNsAliasReference.BindTo(declaredElement);
                }

                // Fix up the whole unquoted type name with namespace
                else if (qualifier is UxmlTypeOrNamespaceReference)
                {
                    if (Token is not XmlIdentifier token) return this;
                    ReferenceWithTokenUtil.SetText(token, token.XmlNameRange, newType.GetClrName().FullName);
                    ((CompositeElementWithReferences)token.Parent)?.ResetReferences();
                }
            }

            return this;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            if (myQualifier == null) // Use the global namespace if there's no qualifier
            {
                var symbolTable = GetGlobalNamespaceSymbolTable();
                return useReferenceName ? symbolTable.Filter(new ExactNameFilter(GetName())) : symbolTable;
            }

            return base.GetReferenceSymbolTable(useReferenceName);
        }

        public QualifierKind GetKind() => Resolve().DeclaredElement is INamespace ? QualifierKind.NAMESPACE : QualifierKind.NONE;

        public override ISymbolTable GetCompletionSymbolTable()
        {
            // this.GetReferenceSymbolTable for an unqualified reference will try to resolve a type based on short
            // name. This is no good for completion. Use the default base behaviour, to show namespaces and types
            // based on the qualifier
            var symbolTable = myQualifier == null
                ? GetGlobalNamespaceSymbolTable()
                : base.GetReferenceSymbolTable(false);
            return symbolTable.Filter(GetCompletionFilters());
        }
        
        private ISymbolTable GetGlobalNamespaceSymbolTable()
        {
            var module = myOwner.GetPsiModule();
            var globalNamespace = mySymbolCache.GetSymbolScope(module, true, CaseSensitive).GlobalNamespace;
            return ResolveUtil.GetSymbolTableByNamespace(globalNamespace, module, true);
        }

        // I(Reference)Qualifier.GetSymbolTable - returns the symbol table of items available from the resolved
        // reference, when being used as a qualifier. Not used to resolve this reference, but can be used to resolve
        // another reference, when this instance is used as a qualifier. E.g. if this reference is a namespace,
        // return all applicable items available in the namespace.
        // Contrast with IReference.GetReferenceSymbolTable, which returns the symbol table used to resolve a
        // reference. For a qualified reference, this will call GetQualifier.GetSymbolTable(mode). If there is no
        // qualifier, it gets a symbol table based on current scope
        public ISymbolTable GetSymbolTable(SymbolTableMode mode, IReference reference, bool useReferenceName)
        {
            if(Resolve().DeclaredElement is not INamespace @namespace)
                return EmptySymbolTable.INSTANCE;

            var module = myOwner.GetPsiModule();
            var symbolTable = ResolveUtil.GetSymbolTableByNamespace(@namespace, module, true);
            return useReferenceName ? symbolTable.Filter(new ExactNameFilter(reference.GetName())) : symbolTable;
        }

        public ISymbolTable GetSymbolTable(SymbolTableMode mode)
        {
            return GetSymbolTable(mode, this, true);
        }
        
        public override ISymbolFilter[] GetSymbolFilters()
        {
            return new ISymbolFilter[]
            {
                new DeclaredElementTypeFilter(ResolveErrorType.TYPE_EXPECTED, CLRDeclaredElementType.NAMESPACE,
                    CLRDeclaredElementType.CLASS),
                myTypeFilter
            };
        }

        public bool Resolved => Resolve().DeclaredElement != null;
    }
}
