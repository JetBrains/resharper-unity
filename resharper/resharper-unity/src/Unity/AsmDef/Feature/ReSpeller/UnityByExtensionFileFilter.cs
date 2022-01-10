using System;
using JetBrains.ReSharper.Features.ReSpeller.Analyzers;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.ReSpeller
{
    [Language(typeof(JsonNewLanguage))]
    public class UnityByExtensionFileFilter : ITypoAnalyzerFileFilter
    {
        public bool ShouldSkipFile(IFile file)
        {
            var path = file.GetSourceFile()?.GetLocation();
            return !path.IsNullOrEmpty() && path.ExtensionNoDot.Equals("asmdef", StringComparison.OrdinalIgnoreCase);
        }
    }
}