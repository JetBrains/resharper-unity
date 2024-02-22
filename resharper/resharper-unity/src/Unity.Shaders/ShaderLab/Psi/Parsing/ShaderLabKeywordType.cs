namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing
{
    public enum ShaderLabKeywordType
    {
        Unknown,
        RegularCommand,
        BlockCommand,
        PropertyType,
        CommandArgument
    }

    public static class ShaderLabKeywordTypeEx
    {
        public static bool IsCommandKeyword(this ShaderLabKeywordType keywordType) => keywordType is ShaderLabKeywordType.RegularCommand or ShaderLabKeywordType.BlockCommand;
    }
}