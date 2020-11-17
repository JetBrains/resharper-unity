using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Refactorings.Rename
{
    public static class UnityRenameUtil
    {
        public static bool IsRenameShouldBeSilent(IDeclaredElement declaredElement)
        {
            var project = declaredElement.GetSourceFiles().FirstOrDefault()?.GetProject();
            if (project == null)
                return false;

            if (project.IsPlayerProject())
                return true;

            if (project.IsMiscProjectItem())
                return true;
            
            return false;
        }
    }
}