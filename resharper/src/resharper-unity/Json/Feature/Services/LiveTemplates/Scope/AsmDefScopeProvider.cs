using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.LiveTemplates.Scope
{
    // ReSharper doesn't have a scope provider for JSON files, whether they are .json or identified as JSON some other
    // way, such as JSON schema's catalog.json, or ReSharper JSON PSI registration. Without this, JSON files don't get
    // any scope, and so don't match any expected scopes in macros and don't get macro expansion in live templates.
    // This scope provider simply returns InAnyLanguageFile for asmdef files, meaning any macros that say they work in
    // any file will also work in asmdef files. If we really wanted to, we could add an InJsonFile or InAsmDefFile scope
    // but if we really needed a scope to work in the template, we could use a file mask scope of `*.asmdef`
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