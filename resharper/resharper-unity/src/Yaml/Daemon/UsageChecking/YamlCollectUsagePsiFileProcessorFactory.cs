using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading.Tasks;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking
{
    [Language(typeof(YamlLanguage))]
    public class YamlCollectUsagePsiFileProcessorFactory : CollectUsagesPsiFileProcessorFactory
    {
        public override ICollectUsagesPsiFileProcessor CreatePsiFileProcessor(CollectUsagesStageProcess collectUsagesStageProcess,
            IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore, CollectUsagesStageProcess.PersistentData persistentData,
            TaskBarrier fibers, IReadOnlyList<IFile> psiFiles, IScopeProcessorFactory scopeProcessorFactory)
        {
            return new YamlCollectUsagesPsiFileProcessor();
        }
    }
}