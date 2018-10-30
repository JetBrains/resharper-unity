using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    [ShellComponent]
    public class UnityScopeProvider : ScopeProvider
    {
        public UnityScopeProvider()
        {
            // Used when creating scope point from settings
            Creators.Add(TryToCreate<InUnityShaderLabFile>);
        }

        public override IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            var sourceFile = context.SourceFile;
            if (sourceFile == null)
                yield break;

            var caretOffset = context.CaretOffset;
            var prefix = LiveTemplatesManager.GetPrefix(caretOffset);

            var documentOffset = caretOffset - prefix.Length;
            if (!documentOffset.IsValid())
                yield break;

            var file = sourceFile.GetPsiFile<ShaderLabLanguage>(documentOffset);
            if (file == null || !file.Language.Is<ShaderLabLanguage>())
                yield break;

            yield return new InUnityShaderLabFile();
        }
    }
}