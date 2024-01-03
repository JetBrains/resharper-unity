using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [Language(typeof(UnityYamlLanguage), Instantiation.DemandAnyThreadSafe)]
    public class UnityYamlUsageCheckingServices : UsageCheckingServices
    {
        public UnityYamlUsageCheckingServices(Lifetime lifetime, IEnumerable<IUsageInspectionsSuppressor> suppressors, 
            IEnumerable<ICustomUsageAnalysisProcessor> customUsageAnalysisProcessors)
            : base(new UsageAnalyzer(suppressors, customUsageAnalysisProcessors))
        {
        }

        // Despite the name, this is about calculating type declarations and hierarchies. We don't have them in YAML.
        // For most YAML files this doesn't matter, but it's a big deal for large files as it will open all chameleons.
        public override bool UseUnknownLanguageStage => false;
    }
}