namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.Impl
{
    internal partial class Identifier
    {
        public string Name => GetText(); // TODO: intern on parsing
    }
}