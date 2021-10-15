using System.Text.RegularExpressions;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    public static class DefineSymbolUtilities
    {
        private const string PreProcessorSymbolPattern = @"\p{L}[\p{L}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]*";
        private const string PreProcessorSymbolExpressionPattern = @"!?" + PreProcessorSymbolPattern;

        private static readonly Regex ourDefineConstraintExpressionRegex =
            new(@"^(?<symbol>" + PreProcessorSymbolExpressionPattern + @")((\s+\|\|\s+)(?<symbol>" + PreProcessorSymbolExpressionPattern + @"))*$",
                RegexOptions.Compiled);

        public static bool IsValidDefineConstraintExpression(string expression) =>
            ourDefineConstraintExpressionRegex.IsMatch(expression);

        public static Match MatchDefineConstraintExpression(string expression) =>
            ourDefineConstraintExpressionRegex.Match(expression);
    }
}