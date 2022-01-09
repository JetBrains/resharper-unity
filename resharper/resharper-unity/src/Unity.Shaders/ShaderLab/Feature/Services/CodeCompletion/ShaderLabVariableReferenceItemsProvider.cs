using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabVariableReferenceItemsProvider : ItemsProviderWithSymbolTable<ShaderLabCodeCompletionContext,
        IVariableReferenceReference, IShaderLabFile>
    {
        protected override TextLookupRanges EvaluateRanges(ShaderLabCodeCompletionContext context)
        {
            return context.Ranges;
        }

        protected override IVariableReferenceReference GetReference(ShaderLabCodeCompletionContext context)
        {
            return context.UnterminatedContext.Reference as IVariableReferenceReference;
        }

        protected override ISymbolTable GetCompletionSymbolTable(IVariableReferenceReference reference, ShaderLabCodeCompletionContext context)
        {
            return reference.GetCompletionSymbolTable();
        }
    }
}