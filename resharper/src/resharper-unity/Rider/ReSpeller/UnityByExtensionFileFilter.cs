using System;
using JetBrains.ReSharper.Features.ReSpeller.Analyzers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.ReSpeller
{
    [Language(typeof (KnownLanguage))]
    public class UnityByExtensionFileFilter : ITypoAnalyzerFileFilter
    {
        public bool ShouldSkipFile(IFile file)
        {
            var path = file.GetSourceFile()?.GetLocation();
            if (FileSystemPathEx.IsNullOrEmpty(path))
                return false;
            return path.ExtensionNoDot.Equals("asmdef", StringComparison.OrdinalIgnoreCase);
        }
    }
}