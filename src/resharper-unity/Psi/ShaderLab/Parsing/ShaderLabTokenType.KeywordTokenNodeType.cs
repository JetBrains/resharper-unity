namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public partial class ShaderLabTokenType
    {
        private class KeywordTokenNodeType : FixedTokenNodeType
        {
            public KeywordTokenNodeType(string s, int index, string representation)
                : base(s, index, representation)
            {
            }

            public override bool IsKeyword => true;
        }
    }
}