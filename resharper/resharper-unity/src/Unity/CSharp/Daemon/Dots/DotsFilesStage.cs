using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Dots
{
    [DaemonStage(StagesBefore = new[] { typeof(CollectUsagesStage) }, StagesAfter = new[] { typeof(CSharpErrorStage) })]
    public class DotsFilesStage : CSharpDaemonStageBase
    {
        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null)
                return false;
            
            var solution = sourceFile.GetSolution();

            if (!solution.HasUnityReference())
                return false;

            var projectFile = sourceFile.ToProjectFile();
            var project = projectFile?.GetProject();
            if (project != null && !project.IsUnityProject())
                return false;

            var packageManager = solution.GetComponent<PackageManager>();
            if (!packageManager.HasPackage(PackageManager.UnityEntitiesPackageName))
                return false;
            
            return base.IsSupported(sourceFile);
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            var typeUsageProcess = process.GetStageProcess<CollectUsagesStageProcess>();
            if (typeUsageProcess == null)
                return null;

            return new DotsFilesStageProcess(typeUsageProcess, process, file);
        }
    }
}