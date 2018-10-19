namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes
{
    internal class CgFilteredGenericTokenNodeType : CgGenericTokenNodeType
    {
        public CgFilteredGenericTokenNodeType(string s, int index, string representation)
            : base(s, index, representation)
        {
        }

        public override bool IsFiltered => true;
    }
}