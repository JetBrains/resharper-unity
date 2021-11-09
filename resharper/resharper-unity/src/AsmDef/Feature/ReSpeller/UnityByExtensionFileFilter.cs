using JetBrains.ReSharper.Features.ReSpeller.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.Utils;
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