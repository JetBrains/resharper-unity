#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Breadcrumbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCodeBlockBase : ShaderLabKeywordDeclarationBase, ICodeBlock, IHierarchicalDeclaration
    {
        protected sealed override ITokenNode Keyword => ((ICodeBlock)this).StartKeyword;
        public IHierarchicalDeclaration? ParentDeclaration => GetContainingNode<IHierarchicalDeclaration>();
        public IEnumerable<IHierarchicalDeclaration> GetChildDeclarations() => EmptyList<IHierarchicalDeclaration>.Enumerable;

        #region IIncludeBlock ahead implementation, can't make abstract because CodeGen doesn't support overrides generation 
        ICgContent ICodeBlock.Content => throw new NotImplementedException();
        ITokenNode ICodeBlock.EndKeyword => throw new NotImplementedException();
        ITokenNode ICodeBlock.StartKeyword => throw new NotImplementedException();
        ICgContent ICodeBlock.SetContent(ICgContent param) => throw new NotImplementedException();
        #endregion
    }
}