using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.UsageChecking
{
    [Language(typeof(YamlLanguage))]
    public class UnityYamlUsageCheckingServices : UsageCheckingServices
    {
        public UnityYamlUsageCheckingServices([NotNull] UsageAnalyzer usageAnalyzer)
            : base(usageAnalyzer)
        {
        }

        // Despite the name, this is about calculating type declarations and hierarchies. We don't have them in YAML.
        // For most YAML files this doesn't matter, but it's a big deal for large files as it will open all chameleons.
        public override bool UseUnknownLanguageStage => false;
    }
}