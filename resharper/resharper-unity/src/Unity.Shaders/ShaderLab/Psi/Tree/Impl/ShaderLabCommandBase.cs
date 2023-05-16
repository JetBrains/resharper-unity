#nullable enable
using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCommandBase : ShaderLabKeywordDeclarationBase, IShaderLabCommand
    {
        protected override DeclaredElementType ElementType => ShaderLabDeclaredElementType.Command; 

        protected sealed override ITokenNode? Keyword => ((IShaderLabCommand)this).CommandKeyword;
        
        #region IShaderLabCommand
        /// <summary>Push <see cref="IShaderLabCommand"/> interface down by hierarchy. Can't just make it abstract, because generator doesn't support overriding of existing methods.</summary>
        ITokenNode IShaderLabCommand.CommandKeyword => throw new NotImplementedException("CommandKeyword should be implemented in derived class");
        #endregion
    }
}