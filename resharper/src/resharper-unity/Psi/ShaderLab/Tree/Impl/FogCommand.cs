namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Tree.Impl
{
    internal partial class FogCommand
    {
        IBlockValue IBlockCommand.Value => Value as IBlockValue;

        public IBlockValue SetValue(IBlockValue param)
        {
            return SetValue((IShaderLabTreeNode) param) as IBlockValue;
        }
    }
}