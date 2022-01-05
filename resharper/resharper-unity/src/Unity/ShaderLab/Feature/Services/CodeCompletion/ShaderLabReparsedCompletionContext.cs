using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    public class ShaderLabReparsedCompletionContext : ReparsedCodeCompletionContext
    {
        public ShaderLabReparsedCompletionContext(IShaderLabFile file, TreeTextRange selectedTreeRange, string newText)
            : base(file, selectedTreeRange, newText)
        {
        }

        protected override IReparseContext GetReparseContext(IFile file, TreeTextRange range)
        {
            return new TrivialReparseContext(file, range);
        }
    }
}