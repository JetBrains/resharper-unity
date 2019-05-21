using System;
using System.Runtime.InteropServices;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Templates.Macros
{
    [
        MacroDefinition("getAlphaNumericFileNameWithoutSuffix", ShortDescription = "Current file name without suffix with all non-alphanumeric replaced with underscores",
            LongDescription = "Evaluates current file name without suffix with all non-alphanumeric replaced with underscores")]
    public class AlphaNumericFileNameWithoutSuffixMacroDef: SimpleMacroDefinition
    {
        public override ParameterInfo[] Parameters => new[] {new ParameterInfo(ParameterType.String)};
    }
  
    [MacroImplementation(Definition = typeof(AlphaNumericFileNameWithoutSuffixMacroDef), ScopeProvider = typeof(PsiImpl))]
    public class AlphaNumericFileNameWithoutSuffixMacroImpl : SimpleMacroImplementation
    {
        private readonly IMacroParameterValueNew myArgument;

        public AlphaNumericFileNameWithoutSuffixMacroImpl([Optional] MacroParameterValueCollection arguments)
        {
            myArgument = arguments.OptionalFirstOrDefault();
        }
        
        
        public override HotspotItems GetLookupItems(IHotspotContext context)
        {
            var suffix = myArgument.GetValue() ?? String.Empty;
            var sourceFile = context.ExpressionRange.Document.GetPsiSourceFile(context.SessionContext.Solution);
            var result =  sourceFile?.GetLocation().NameWithoutExtension.RemoveEnd(suffix);
            return MacroUtil.SimpleEvaluateResult(result);
        }
    }
}