namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Tree.Impl
{
    internal partial class Identifier
    {
        public string Name => GetText(); // TODO: intern on parsing
    }
}