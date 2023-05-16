using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabBlockCommandBase : ShaderLabCommandBase, IHierarchicalDeclaration, IBlockCommand
    {
        public IHierarchicalDeclaration ParentDeclaration => GetContainingNode<IHierarchicalDeclaration>();
        public IEnumerable<IHierarchicalDeclaration> GetChildDeclarations() => ((IBlockCommand)this).Value.Children<IHierarchicalDeclaration>();

        #region IBlockCommand ahead declaration
        IBlockValue IBlockCommand.Value => throw new NotImplementedException();
        IBlockValue IBlockCommand.SetValue(IBlockValue param) => throw new NotImplementedException();
        #endregion
    }
}