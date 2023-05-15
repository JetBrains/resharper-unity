#nullable enable
using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabCommandBase : ShaderLabDeclarationBase, IShaderLabCommand
    {
        private readonly CachedPsiValueWithOffsets<IDeclaredElement> myCachedDeclaredElement = new();

        public sealed override IDeclaredElement DeclaredElement => myCachedDeclaredElement.GetValue(this, static self => self.CreateDeclaredElement());
        public override string DeclaredName => (CommandKeyword?.NodeType as ITokenNodeType)?.TokenRepresentation ?? SharedImplUtil.MISSING_DECLARATION_NAME; 
        public override void SetName(string name) => throw new NotSupportedException();
        public override TreeTextRange GetNameRange() => CommandKeyword?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;

        protected virtual IDeclaredElement CreateDeclaredElement() => new SimpleShaderLabDeclaredElement(DeclaredName, GetSourceFile()!, GetTreeStartOffset().Offset, ShaderLabDeclaredElementType.Command);

        #region IShaderLabCommand
        /// <summary>Push <see cref="IShaderLabCommand"/> interface down by hierarchy. Can't just make it abstract, because generator doesn't support overriding of existing methods.</summary>
        ITokenNode IShaderLabCommand.CommandKeyword => throw new NotImplementedException("CommandKeyword should be implemented in derived class");
        private ITokenNode? CommandKeyword => ((IShaderLabCommand)this).CommandKeyword;
        #endregion
    }
}