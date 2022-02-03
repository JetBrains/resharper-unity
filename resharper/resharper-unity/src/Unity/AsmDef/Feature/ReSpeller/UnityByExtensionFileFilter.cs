using JetBrains.ReSharper.Features.ReSpeller.Analyzers;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.ReSpeller
{
    [Language(typeof(JsonNewLanguage))]
    public class UnityByExtensionFileFilter : ITypoAnalyzerFileFilter
    {
        public bool ShouldSkipFile(IFile file)
        {
            var sourceFile = file.GetSourceFile();
            return sourceFile != null && (sourceFile.IsAsmDef() || sourceFile.IsAsmRef());
        }
    }
}