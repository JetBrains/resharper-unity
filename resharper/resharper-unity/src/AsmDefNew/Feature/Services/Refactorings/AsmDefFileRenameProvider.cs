using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Refactorings;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.DeclaredElements;
using JetBrains.ReSharper.Refactorings.Rename;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Refactorings
{
    // Support renaming the asmdef file to match the name of the assembly
    [FileRenameProvider]
    public class AsmDefFileRenameProvider : AsmDefFileRenameProviderBase<AsmDefNameDeclaredElement>
    {
        // TODO: Copied from R# JSON based implementation
        // public IEnumerable<FileRename> GetFileRenames(IDeclaredElement declaredElement, string name)
        // {
            // if (declaredElement is AsmDefNameDeclaredElement)
            // {
                // var sourceFile = declaredElement.GetSourceFiles().FirstOrDefault();
                // if (sourceFile != null)
                // {
                    // var psiServices = declaredElement.GetPsiServices();
                    // var projectFile = sourceFile.ToProjectFile();
                    // return new[] {new FileRename(psiServices, projectFile, name)};
                // }
            // }

            // return EmptyList<FileRename>.Enumerable;
        // }
    }
}