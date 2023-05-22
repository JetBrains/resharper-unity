#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Services.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCodeBlockBase : ShaderLabKeywordDeclarationBase, ICodeBlock, IStructuralDeclaration
    {
        protected sealed override ITokenNode Keyword => ((ICodeBlock)this).StartKeyword;
        public IStructuralDeclaration? ContainingDeclaration => GetContainingNode<IStructuralDeclaration>();
        public IEnumerable<IStructuralDeclaration> GetMemberDeclarations() => EmptyList<IStructuralDeclaration>.Enumerable;

        #region IIncludeBlock ahead implementation, can't make abstract because CodeGen doesn't support overrides generation 
        ICgContent ICodeBlock.Content => throw new NotImplementedException();
        ITokenNode ICodeBlock.EndKeyword => throw new NotImplementedException();
        ITokenNode ICodeBlock.StartKeyword => throw new NotImplementedException();
        ICgContent ICodeBlock.SetContent(ICgContent param) => throw new NotImplementedException();
        #endregion
    }
}