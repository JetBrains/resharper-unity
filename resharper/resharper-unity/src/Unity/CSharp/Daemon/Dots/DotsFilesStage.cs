using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Dots
{
    [DaemonStage(StagesBefore = new[] { typeof(CollectUsagesStage) }, StagesAfter = new[] { typeof(CSharpErrorStage) })]
    public class DotsFilesStage : IDaemonStage
    {
        public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind)
        {
            var solution = process.Solution;
            if (!solution.HasUnityReference())
                return EmptyArray<IDaemonStageProcess>.Instance;

            var projectFile = process.SourceFile.ToProjectFile();
            var project = projectFile?.GetProject();
            if (project != null && !project.IsUnityProject())
                return EmptyArray<IDaemonStageProcess>.Instance;

            var packageManager = solution.GetComponent<PackageManager>();
            if (!packageManager.HasPackage(PackageManager.UnityEntitiesPackageName))
                return EmptyArray<IDaemonStageProcess>.Instance;

            var typeUsageProcess = process.GetStageProcess<CollectUsagesStageProcess>();
            if (typeUsageProcess == null)
                return EmptyArray<IDaemonStageProcess>.Instance;

            return process.SourceFile.GetPsiFiles<CSharpLanguage>()
                .Select(file => new DotsFilesStageProcess(typeUsageProcess, process, file));
        }
    }
}