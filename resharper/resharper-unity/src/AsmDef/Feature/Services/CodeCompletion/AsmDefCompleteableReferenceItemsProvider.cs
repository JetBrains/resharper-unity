using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Feature.CodeCompletion;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.CodeCompletion
{
    [Language(typeof(JsonNewLanguage))]
    public class AsmDefCompleteableReferenceItemsProvider : ItemsProviderWithSymbolTable<JsonNewCodeCompletionContext, AsmDefNameReference, IJsonNewFile>
    {
        protected override TextLookupRanges EvaluateRanges(JsonNewCodeCompletionContext context) => context.Ranges;

        protected override AsmDefNameReference GetReference(JsonNewCodeCompletionContext context) =>
            context.UnterminatedContext.Reference as AsmDefNameReference;

        protected override ISymbolTable GetCompletionSymbolTable(AsmDefNameReference reference,
                                                                 JsonNewCodeCompletionContext context)
        {
            return reference.GetCompletionSymbolTable();
        }
    }
}