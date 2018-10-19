using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.Resolve.Filters;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Impl.Shared.References;
using JetBrains.ReSharper.Psi.Impl.Shared.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resx.Utils;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Resolve
{
    public class UnityObjectTypeOrNamespaceReference :
        QualifiableReferenceWithinElement<ICSharpLiteralExpression, ITokenNode>,
        IReferenceQualifier, ICompletableReference
    {
        private readonly ISymbolCache mySymbolCache;
        private readonly bool myIsFinalPart;
        private readonly ForwardedTypesFilter myForwardedTypesFilter;
        private readonly ExpectedObjectTypeFilter myExpectedObjectTypeFilter;

        public UnityObjectTypeOrNamespaceReference(ICSharpLiteralExpression owner, [CanBeNull] IQualifier qualifier,
                                                   ITokenNode token, TextRange rangeWithin,
                                                   ExpectedObjectTypeReferenceKind kind, ISymbolCache symbolCache,
                                                   bool isFinalPart)
            : base(owner, qualifier, token, rangeWithin.ToTreeTextRange())
        {
            mySymbolCache = symbolCache;
            myIsFinalPart = isFinalPart;
            myForwardedTypesFilter = new ForwardedTypesFilter(symbolCache);
            myExpectedObjectTypeFilter = new ExpectedObjectTypeFilter(kind, mustBeClass: isFinalPart);
        }

        public override Staticness GetStaticness() => Staticness.Any;

        // Qualifier is either null or a namespace. No type element
        public override ITypeElement GetQualifierTypeElement() => null;

        protected override IReference BindToInternal(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            // Fix up name
            if (declaredElement.ShortName != GetName())
            {
                var newReference = ReferenceWithinElementUtil<ITokenNode>.SetText(this, declaredElement.ShortName,
                    (node, buffer) =>
                    {
                        // The new name is substituted into the existing text, which includes quotes
                        var unquotedStringValue = buffer.GetText(TextRange.FromLength(1, buffer.Length - 2));
                        return CSharpElementFactory.GetInstance(node)
                            .CreateStringLiteralExpression(unquotedStringValue)
                            .Literal;
                    });
                return newReference.BindTo(declaredElement);
            }

            // Fix up qualification (e.g. move namespace)
            if (declaredElement is ITypeElement newType && !newType.Equals(Resolve().DeclaredElement))
            {
                var oldRange = new TreeTextRange(TreeOffset.Zero, new TreeOffset(myOwner.GetTextLength()));
                ReferenceWithinElementUtil<ITokenNode>.SetText(myOwner.Literal, oldRange,
                    newType.GetClrName().FullName, (node, buffer) =>
                    {
                        // We're replacing the whole text and don't provide quotes in the new string
                        var unquotedStringValue = buffer.GetText();
                        return CSharpElementFactory.GetInstance(node)
                            .CreateStringLiteralExpression(unquotedStringValue)
                            .Literal;
                    }, myOwner);
                var newReference = myOwner.FindReference<UnityObjectTypeOrNamespaceReference>(r => r.myIsFinalPart);
                Assertion.AssertNotNull(newReference, "newReference != null");
                return newReference;
            }

            return this;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            if (myQualifier == null && myIsFinalPart)
            {
                var name = GetName();
                var symbolScope = mySymbolCache.GetSymbolScope(LibrarySymbolScope.FULL, true);
                var declaredElements = symbolScope.GetElementsByShortName(name);
                var symbolTable = ResolveUtil.CreateSymbolTable(declaredElements, 0);

                // GetElementsByShortName is case insensitive, so filter by exact name, which is case sensitive.
                // Then filter out any forwarded types. Unity has a lot of these, with types forwarded from
                // UnityEngine.dll to UnityEngine.*Module.dll
                // Note that filtering here means that filtered values are not treated as candidates and the errors
                // are not shown. Use GetSymbolFilters for that
                return symbolTable.Filter(new ExactNameFilter(name), myForwardedTypesFilter);
            }

            // Use the global namespace if there's no qualifier. The base implementation would create a scope based on
            // local using statements
            if (myQualifier == null)
            {
                var symbolTable = GetGlobalNamespaceSymbolTable();
                return useReferenceName ? symbolTable.Filter(new ExactNameFilter(GetName())) : symbolTable;
            }

            return base.GetReferenceSymbolTable(useReferenceName);
        }

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

        // We have a number of symbol filters and it can be confusing to know when they're used and what the default
        // implementations are. For QualifiableReferenceWithinElement:
        // * GetSymbolFilters - used for resolve. Default is to call GetSmartSymbolFilters and add ExactNameFilter
        //   Typically, items filtered here are treated as candidates, and errors are used. Anything filtered in
        //   GetReferenceSymbolTable is lost, and errors are ignored
        // * GetSmartSymbolFilters - used to filter the reference symbol table in GetSmartCompletionTable (but GSCT
        //   is not a common interface method, so only used/implemented by certain languages if we add an interface)
        //   Default is to return GetCompletionFilters
        // * GetCompletionFilters - used by base.GetCompletionTable(), which is only called if this reference adds
        //   ICompletableReference. Default is an empty list
        public override ISymbolFilter[] GetSymbolFilters()
        {
            return new ISymbolFilter[]
            {
                new DeclaredElementTypeFilter(ResolveErrorType.TYPE_EXPECTED, CLRDeclaredElementType.NAMESPACE,
                    CLRDeclaredElementType.CLASS),
                myExpectedObjectTypeFilter,
                IsNonGenericFilter.INSTANCE
            };
        }

        protected override ISymbolFilter[] GetCompletionFilters()
        {
            // Complete types and namespaces, no matter where we are. If we're at the end of the string, the user
            // might be trying to complete a type in the namespace from the current qualifier, or might be trying to
            // add another namespace. If we're in the middle of the string, they might be changing their mind, and
            // adding a new (qualified) type, so adding either type or namespace (the text to the right will fail to
            // resolve)
            return new ISymbolFilter[]
            {
                CSharpTypeOrNamespaceFilter.INSTANCE,
                TypeElementMustBeClassFilter.INSTANCE,
                new GlobalUnityEditorTypeFilter(),
                IsPublicFilter.INSTANCE
            };
        }

        public QualifierKind GetKind()
        {
            return Resolve().DeclaredElement is INamespace ? QualifierKind.NAMESPACE : QualifierKind.NONE;
        }

        public bool Resolved => Resolve().DeclaredElement != null;

        private class GlobalUnityEditorTypeFilter : SimpleSymbolFilter
        {
            public override ResolveErrorType ErrorType => ResolveErrorType.IGNORABLE;

            public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
            {
                var typeElement = declaredElement as ITypeElement;
                if (typeElement == null)
                    return true;

                // Filter out the obsolete AssetModificationProcessor type in the global namespace from UnityEditor.dll
                if (declaredElement.ShortName == "AssetModificationProcessor")
                    return !typeElement.Module.Name.StartsWith("UnityEditor");

                if (declaredElement.ShortName == "TMPro_SDFMaterialEditor")
                {
                    // This is copied into Libraries, so don't rely on case (UnityEditor.dll comes from the install dir)
                    return !typeElement.Module.Name.Equals("Unity.TextMeshPro.Editor",
                        StringComparison.OrdinalIgnoreCase);
                }

                return true;
            }
        }
    }
}