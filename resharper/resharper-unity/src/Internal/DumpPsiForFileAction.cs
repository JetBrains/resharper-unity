using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Features.Internal.PsiModules;
using JetBrains.ReSharper.Features.Internal.resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Internal
{
    [Action("Dump PSI for Current File")]
    public class DumpPsiForFileAction : IExecutableAction, IInsertBefore<InternalPsiMenu, DumpPsiSourceFilePropertiesAction>
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            var projectFile = context.GetData(ProjectModelDataConstants.PROJECT_MODEL_ELEMENT) as IProjectFile;
            return projectFile?.ToSourceFile() != null;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            var projectFile = context.GetData(ProjectModelDataConstants.PROJECT_MODEL_ELEMENT) as IProjectFile;
            var sourceFile = projectFile?.ToSourceFile();
            if (sourceFile == null)
                return;

            var path = CompanySpecificFolderLocations.TempFolder / (Path.GetRandomFileName() + ".txt");
            using (var sw = new StreamWriter(path.OpenFileForWriting(), Encoding.UTF8))
            {
                foreach (var psiFile in sourceFile.EnumerateDominantPsiFiles())
                {
                    sw.WriteLine("Language: {0}", psiFile.Language.Name);
                    DebugUtil.DumpPsi(sw, psiFile);
                }
            }

            Process.Start(path.FullPath);
        }
    }
}
