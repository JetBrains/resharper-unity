using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Daemon.Stages.Processes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Daemon.Stages
{
    [DaemonStage]
    public class JsonInspectionsStage : JsonNewDaemonStageBase
    {
        private readonly ElementProblemAnalyzerRegistrar myElementProblemAnalyzerRegistrar;

        public JsonInspectionsStage(ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar)
        {
            myElementProblemAnalyzerRegistrar = elementProblemAnalyzerRegistrar;
        }

        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process,
            IContextBoundSettingsStore settings, DaemonProcessKind processKind, IJsonNewFile file)
        {
            return new JsonNewInspectionsProcess(process, settings, file, processKind, myElementProblemAnalyzerRegistrar);
        }
    }
}