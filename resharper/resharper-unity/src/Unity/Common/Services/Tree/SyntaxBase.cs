#nullable enable
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree
{
    public class SyntaxBase
    {
        public TokenNodeType WHITE_SPACE { get; init; }
        public TokenNodeType NEW_LINE { get; init; }
    }
}