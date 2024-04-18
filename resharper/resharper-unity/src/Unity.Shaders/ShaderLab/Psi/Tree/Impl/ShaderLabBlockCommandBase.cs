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
    public abstract class ShaderLabBlockCommandBase : ShaderLabCommandBase, IBlockCommand
    {
        protected override DeclaredElementType DeclaredElementType => ShaderLabDeclaredElementType.BlockCommand;

        public sealed override IEnumerable<IStructuralDeclaration> GetMemberDeclarations()
        {
            if (((IBlockCommand)this).Value is not {} value) return EmptyList<IStructuralDeclaration>.Enumerable;
            var declarations = new LocalList<IStructuralDeclaration>();
            foreach (var child in value.Children())
            {
                if (child is IStructuralDeclaration declaration)
                    declarations.Add(declaration);
                else
                    CollectCustomChildDeclarations(child, ref declarations);
            }             
            return declarations.ReadOnlyList();
        }

        protected virtual void CollectCustomChildDeclarations(ITreeNode child, ref LocalList<IStructuralDeclaration> declarations) 
        {
        }

        #region IBlockCommand ahead declaration
        IBlockValue IBlockCommand.Value => throw new NotImplementedException();
        IBlockValue IBlockCommand.SetValue(IBlockValue param) => throw new NotImplementedException();
        #endregion
    }
}