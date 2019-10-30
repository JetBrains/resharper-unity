using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Refactorings
{
    // Support renaming the asmdef file to match the name of the assembly
    public abstract class AsmDefFileRenameProviderBase<TDeclaredElement> : IFileRenameProvider where TDeclaredElement : IDeclaredElement
    {
        public IEnumerable<FileRename> GetFileRenames(IDeclaredElement declaredElement, string name)
        {
            if (declaredElement is TDeclaredElement)
            {
                var sourceFile = declaredElement.GetSourceFiles().FirstOrDefault();
                if (sourceFile != null)
                {
                    var psiServices = declaredElement.GetPsiServices();
                    var projectFile = sourceFile.ToProjectFile();
                    return new[] {new FileRename(psiServices, projectFile, name)};
                }
            }

            return EmptyList<FileRename>.Enumerable;
        }
    }
}