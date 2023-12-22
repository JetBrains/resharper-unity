#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCodeBlockBase : ShaderLabDeclarationBase, ICodeBlock, IStructuralDeclaration
    {
        protected abstract DeclaredElementType DeclaredElementType { get; }
        public IStructuralDeclaration? ContainingDeclaration => GetContainingNode<IStructuralDeclaration>();
        public IEnumerable<IStructuralDeclaration> GetMemberDeclarations() => EmptyList<IStructuralDeclaration>.Enumerable;
        public sealed override string? GetName() => ((ICodeBlock)this).StartKeyword?.GetText();
        public sealed override TreeTextRange GetNameRange() => ((ICodeBlock)this).StartKeyword?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;
        protected override IDeclaredElement? TryCreateDeclaredElement() => ((ICodeBlock)this).StartKeyword is { } keyword ? new SimpleShaderLabDeclaredElement(keyword.GetText(), GetSourceFile()!, GetTreeStartOffset().Offset, DeclaredElementType) : null;

        #region IIncludeBlock ahead implementation, can't make abstract because CodeGen doesn't support overrides generation 
        ICgContent ICodeBlock.Content => throw new NotImplementedException();
        ITokenNode? ICodeBlock.EndKeyword => throw new NotImplementedException();
        ITokenNode? ICodeBlock.StartKeyword => throw new NotImplementedException();
        ICgContent ICodeBlock.SetContent(ICgContent param) => throw new NotImplementedException();
        #endregion
    }
}