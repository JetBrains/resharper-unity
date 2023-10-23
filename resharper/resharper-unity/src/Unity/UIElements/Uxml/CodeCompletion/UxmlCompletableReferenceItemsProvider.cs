using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.CodeCompletion
{
    [Language(typeof(UxmlLanguage))]
    internal class UxmlCompletableReferenceItemsProvider : ItemsProviderWithSymbolTable<UxmlCodeCompletionContext, UxmlTypeOrNamespaceReference, IXmlFile>
    {
        protected override TextLookupRanges EvaluateRanges(UxmlCodeCompletionContext context) => context.Ranges;

        protected override UxmlTypeOrNamespaceReference GetReference(UxmlCodeCompletionContext context) =>
            context.UnterminatedContext.Reference as UxmlTypeOrNamespaceReference;

        protected override ISymbolTable GetCompletionSymbolTable(UxmlTypeOrNamespaceReference reference,
            UxmlCodeCompletionContext context)
        {
            return reference.GetCompletionSymbolTable();
        }
    }
}