#nullable enable

using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabIncludeBase : ShaderLabKeywordDeclarationBase, IIncludeBlock
    {
        protected sealed override DeclaredElementType ElementType => ShaderLabDeclaredElementType.IncludeBlock;
        protected sealed override ITokenNode Keyword => ((IIncludeBlock)this).StartKeyword;
        
        #region IIncludeBlock ahead implementation, can't make abstract because CodeGen doesn't support overrides generation 
        ICgContent IIncludeBlock.Content => throw new NotImplementedException();
        ITokenNode IIncludeBlock.EndKeyword => throw new NotImplementedException();
        ITokenNode IIncludeBlock.StartKeyword => throw new NotImplementedException();
        ICgContent IIncludeBlock.SetContent(ICgContent param) => throw new NotImplementedException();
        #endregion
    }
}