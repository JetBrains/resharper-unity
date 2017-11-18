using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates
{
    // ReSharper doesn't have a scope provider for "all PSI files", or for JSON files
    // (either matching .json, with a separate extension, or identified as JSON via
    // something like JSON schema's catalog.json). Without this, JSON files don't get
    // given a scope, and so don't match with expected scopes in macros.
    // As far as I can tell, we don't need to return a specific InJsonFile, or InAsmDefFile
    // unless we want to enable adding Live Templates that are specific to asmdef files
    // (but then we could use a file mask of "*.asmdef" in that template)
    // See RSRP-467094
    [ShellComponent]
    public class AsmDefScopeProvider : ScopeProvider
    {
        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            var sourceFile = context.SourceFile;
            if (sourceFile == null)
                yield break;

            if (sourceFile.PrimaryPsiLanguage.Is<JsonLanguage>() && IsAsmDefFile(sourceFile))
                yield return new InAnyLanguageFile();
        }

        private static bool IsAsmDefFile(IPsiSourceFile sourceFile)
        {
            var location = sourceFile.GetLocation();
            return !location.IsEmpty && location.ExtensionNoDot.Equals("asmdef", StringComparison.OrdinalIgnoreCase);
        }
    }
}