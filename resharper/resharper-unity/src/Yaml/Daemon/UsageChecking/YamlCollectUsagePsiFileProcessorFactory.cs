using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading.Tasks;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.UsageChecking.SwaExtension;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking
{
    // TODO: Introduce UnityYamlLanguage?
    // This is Unity only functionality, so shouldn't be registered as the only YAML language specific service. Up to
    // now, the split between the YAML plugin and Unity has been clean - Unity specific features sit on top of or
    // alongside the YAML plugin, without replacing or overriding components.
    // Perhaps it's time to introduce a specialisation of the YAML language for Unity file types, that can give us extra
    // flexibility for handling things (e.g. checking for Unity YAML files without having to always do a string
    // comparison with file extensions)
    [Language(typeof(UnityYamlLanguage))]
    public class YamlCollectUsagePsiFileProcessorFactory : CollectUsagesPsiFileProcessorFactory
    {
        public override ICollectUsagesPsiFileProcessor CreatePsiFileProcessor(
            CollectUsagesStageProcess collectUsagesStageProcess,
            IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore,
            CollectUsagesStagePersistentData persistentData,
            TaskBarrier fibers, IReadOnlyList<IFile> psiFiles, IScopeProcessorFactory scopeProcessorFactory,
            IEnumerable<SwaExtensionProviderBase> swaExtensionProviders)
        {
            // TODO: We could return a no-op processor for .meta or boring assets
            return new YamlCollectUsagesPsiFileProcessor(daemonProcess);
        }
    }
}