using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.JavaScript;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.CodeCompletion
{
    [Language(typeof(JsonLanguage))]
    public class AsmDefCompleteableReferenceItemsProvider : ItemsProviderWithSymbolTable<JavaScriptCodeCompletionContext, AsmDefNameReference, IJavaScriptFile>
    {
        protected override TextLookupRanges EvaluateRanges(JavaScriptCodeCompletionContext context)
        {
            return context.Ranges;
        }

        protected override AsmDefNameReference GetReference(JavaScriptCodeCompletionContext context)
        {
            return context.UnterminatedContext.Reference as AsmDefNameReference;
        }

        protected override ISymbolTable GetCompletionSymbolTable(AsmDefNameReference reference, JavaScriptCodeCompletionContext context)
        {
            return reference.GetCompletionSymbolTable();
        }
    }
}