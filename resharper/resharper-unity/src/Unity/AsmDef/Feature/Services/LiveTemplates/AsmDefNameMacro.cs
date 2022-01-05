using System.Collections.Generic;
using System.Text;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Macros;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.LiveTemplates
{
    [MacroDefinition("asmDefNameMacro", ShortDescription = "Current file name without whitespace characters",
            LongDescription = "Evaluates current file name without whitespace characters")]
    public class AsmDefNameMacroDef: SimpleMacroDefinition
    {
        public override string GetPlaceholder(IDocument document, ISolution solution, IEnumerable<IMacroParameterValue> parameters)
        {
            return Evaluate(document.GetPsiSourceFile(solution));
        }
    
        public static string Evaluate(IPsiSourceFile sourceFile)
        {
            var sb = new StringBuilder();
            var name = sourceFile?.GetLocation().NameWithoutExtension ?? "";
            for (int i = 0; i < name.Length; i++)
                if (!name[i].IsPureWhitespace())
                    sb.Append(name[i]);
            
            return sb.ToString();
        }
    
        public override bool CanBeEvaluatedWithoutCommit => true;
    }
  
    [MacroImplementation(Definition = typeof(AsmDefNameMacroDef), ScopeProvider = typeof(PsiImpl))]
    public class AlphaNumericFileNameWithoutExtensionMacroImpl : SimpleMacroImplementation
    {
        public override HotspotItems GetLookupItems(IHotspotContext context)
        {
            var sourceFile = context.ExpressionRange.Document.GetPsiSourceFile(context.SessionContext.Solution);
            return MacroUtil.SimpleEvaluateResult(AsmDefNameMacroDef.Evaluate(sourceFile));
        }
    }
}