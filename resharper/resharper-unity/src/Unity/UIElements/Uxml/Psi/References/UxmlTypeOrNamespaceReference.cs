using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Impl.Shared.References;
using JetBrains.ReSharper.Psi.Impl.Shared.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xaml.Impl.Resolve;
using JetBrains.ReSharper.Psi.Xaml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xaml.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References
{
    internal class UxmlTypeOrNamespaceReference :
        QualifiableReferenceWithinElement<IXmlTagHeader, ITokenNode>,
        IReferenceQualifier, ICompletableReference, ITypeOrNamespaceReference
    {
        private readonly ISymbolCache mySymbolCache;
        private readonly bool myIsFinalPart;
        private readonly ExpectedVisualElementTypeFilter myExpectedObjectTypeFilter;

        public UxmlTypeOrNamespaceReference(IXmlTagHeader owner, [CanBeNull] IQualifier qualifier,
                                                   ITokenNode token, TreeTextRange rangeWithin, ISymbolCache symbolCache,
                                                   bool isFinalPart)
            : base(owner, qualifier, token, rangeWithin)
        {
            mySymbolCache = symbolCache;
            myIsFinalPart = isFinalPart;
            myExpectedObjectTypeFilter = new ExpectedVisualElementTypeFilter(mustBeClass: isFinalPart);
        }

        public override Staticness GetStaticness() => Staticness.Any;

        // Qualifier is either null or a namespace. No type element
        public override ITypeElement GetQualifierTypeElement() => null;

        protected override IReference BindToInternal(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            // Fix up name
            // if (declaredElement.ShortName != GetName())
            // {
            //     var newReference = ReferenceWithinElementUtil<ITokenNode>.SetText(this, declaredElement.ShortName,
            //         (node, buffer) =>
            //         {
            //             // The new name is substituted into the existing text, which includes quotes
            //             var unquotedStringValue = buffer.GetText(TextRange.FromLength(1, buffer.Length - 2));
            //             return CSharpElementFactory.GetInstance(node)
            //                 .CreateStringLiteralExpression(unquotedStringValue)
            //                 .Literal;
            //         });
            //     return newReference.BindTo(declaredElement);
            // }

            return this;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            if (myQualifier is IXamlNamespaceAliasReference xamlNamespaceAliasReference)
            { 
                return XamlResolveUtil.GetNamespaceAliasSymbolTable(xamlNamespaceAliasReference);
                return xamlNamespaceAliasReference.GetSymbolTable(SymbolTableMode.FULL);
            }
            
            if (myQualifier == null) // Use the global namespace if there's no qualifier
            {
                var module = myOwner.GetPsiModule();
                var globalNamespace = mySymbolCache.GetSymbolScope(module, true, CaseSensitive).GlobalNamespace;
                var symbolTable = ResolveUtil.GetSymbolTableByNamespace(globalNamespace, module, true);
                return useReferenceName ? symbolTable.Filter(new ExactNameFilter(GetName())) : symbolTable;
            }

            return base.GetReferenceSymbolTable(useReferenceName);
        }

        public QualifierKind GetKind() => Resolve().DeclaredElement is INamespace ? QualifierKind.NAMESPACE : QualifierKind.NONE;

        // public override ISymbolTable GetCompletionSymbolTable()
        // {
        //     // this.GetReferenceSymbolTable for an unqualified reference will try to resolve a type based on short
        //     // name. This is no good for completion. Use the default base behaviour, to show namespaces and types
        //     // based on the qualifier
        //     var symbolTable = myQualifier == null
        //         ? GetGlobalNamespaceSymbolTable()
        //         : base.GetReferenceSymbolTable(false);
        //     return symbolTable.Filter(GetCompletionFilters());
        // }

        // I(Reference)Qualifier.GetSymbolTable - returns the symbol table of items available from the resolved
        // reference, when being used as a qualifier. Not used to resolve this reference, but can be used to resolve
        // another reference, when this instance is used as a qualifier. E.g. if this reference is a namespace,
        // return all applicable items available in the namespace.
        // Contrast with IReference.GetReferenceSymbolTable, which returns the symbol table used to resolve a
        // reference. For a qualified reference, this will call GetQualifier.GetSymbolTable(mode). If there is no
        // qualifier, it gets a symbol table based on current scope
        public ISymbolTable GetSymbolTable(SymbolTableMode mode, IReference reference, bool useReferenceName)
        {
            if(!(Resolve().DeclaredElement is INamespace @namespace))
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
                myExpectedObjectTypeFilter
            };
        }

        public bool Resolved => Resolve().DeclaredElement != null;
    }
}
