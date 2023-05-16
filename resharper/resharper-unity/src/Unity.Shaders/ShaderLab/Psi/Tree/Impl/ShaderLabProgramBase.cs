using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabProgramBase : ShaderLabKeywordDeclarationBase, IProgramBlock
    {
        protected sealed override DeclaredElementType ElementType => ShaderLabDeclaredElementType.ProgramBlock;
        protected sealed override ITokenNode Keyword => ((IProgramBlock)this).StartKeyword;

        #region IProgramBlock ahead implementation, can't make abstract because CodeGen doesn't support overrides generation 
        ICgContent IProgramBlock.Content => throw new NotImplementedException();
        ITokenNode IProgramBlock.EndKeyword => throw new NotImplementedException();
        ITokenNode IProgramBlock.StartKeyword => throw new NotImplementedException();
        ICgContent IProgramBlock.SetContent(ICgContent param) => throw new NotImplementedException();
        #endregion
    }
}