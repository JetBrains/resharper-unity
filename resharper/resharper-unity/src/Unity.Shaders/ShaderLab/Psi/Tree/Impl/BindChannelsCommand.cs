using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class BindChannelsCommand
    {
        public override IEnumerable<IHierarchicalDeclaration> GetChildDeclarations()
        {
            if (Value is not IBindChannelsValue bindChannelsValue)
                return EmptyList<IHierarchicalDeclaration>.Enumerable;
            return bindChannelsValue.BindCommandEnumerable.OfType<IHierarchicalDeclaration>();
        }

        IBlockValue IBlockCommand.Value => Value as IBlockValue;

        public IBlockValue SetValue(IBlockValue param)
        {
            return SetValue((IShaderLabTreeNode) param) as IBlockValue;
        }
    }
}