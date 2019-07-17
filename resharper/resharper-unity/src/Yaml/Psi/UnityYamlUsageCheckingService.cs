using System;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [Language(typeof(UnityYamlLanguage))]
    public class UnityYamlUsageCheckingServices : UsageCheckingServices
    {
        public UnityYamlUsageCheckingServices(Lifetime lifetime, IViewable<IUsageInspectionsSuppressor> suppressors)
            : base(new UsageAnalyzer(lifetime, suppressors))
        {
        }

        // Despite the name, this is about calculating type declarations and hierarchies. We don't have them in YAML.
        // For most YAML files this doesn't matter, but it's a big deal for large files as it will open all chameleons.
        public override bool UseUnknownLanguageStage => false;
    }
}