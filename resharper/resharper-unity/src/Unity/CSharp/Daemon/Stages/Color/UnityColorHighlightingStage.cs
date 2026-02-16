using System.Collections;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color
{
    [DaemonStage(Instantiation.DemandAnyThreadSafe,
        StagesBefore = [typeof(IdentifierHighlightingStage)],
        HighlightingTypes = [typeof(UnityColorHighlighterProcess)])]
    public class UnityColorHighlightingStage : CSharpDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (settings.GetValue(HighlightingSettingsAccessor.ColorUsageHighlightingEnabled))
            {
                var unityColorReferenceProviders = file.GetSolution().GetComponents<IUnityColorReferenceProvider>();
                return new UnityColorHighlighterProcess(
                    unityColorReferenceProviders, process, settings, file);
            }

            return null;
        }

        public override bool IsApplicable(IPsiSourceFile sourceFile, DaemonProcessKind processKind)
        {
            return processKind == DaemonProcessKind.VISIBLE_DOCUMENT
                   && sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }
}