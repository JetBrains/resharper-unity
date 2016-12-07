using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public static class ParserMessages
    {
        public const string Foo = "Whatever";

        public static string GetString(string id) => id;

        public static string GetUnexpectedTokenMessage() => "Unexpected token";

        public static string GetExpectedMessage(string expectedSymbol)
        {
            return string.Format(GetString("{0} expected"), expectedSymbol).Capitalize();
        }
    }
}