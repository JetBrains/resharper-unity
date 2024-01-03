#nullable enable
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabTexturePassItemsProvider : ItemsProviderWithSymbolTable<ShaderLabCodeCompletionContext, ITexturePassReference, IShaderLabFile>
    {
        protected override TextLookupRanges EvaluateRanges(ShaderLabCodeCompletionContext context) => context.CompletionRanges;

        protected override ITexturePassReference? GetReference(ShaderLabCodeCompletionContext context) => context.UnterminatedContext.Reference as ITexturePassReference;

        protected override ISymbolTable GetCompletionSymbolTable(ITexturePassReference reference, ShaderLabCodeCompletionContext context) => reference.GetCompletionSymbolTable();
    }
}