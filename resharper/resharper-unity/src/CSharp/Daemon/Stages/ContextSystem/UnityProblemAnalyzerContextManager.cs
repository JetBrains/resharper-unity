using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [SolutionComponent]
    public class UnityProblemAnalyzerContextManager
    {
        public readonly IReadOnlyList<IUnityProblemAnalyzerContextProvider> Providers;

        public UnityProblemAnalyzerContextManager(IEnumerable<IUnityProblemAnalyzerContextProvider> providers)
        {
            Providers = providers.ToList();

            Providers.AssertClassifications();
        }

        public UnityProblemAnalyzerContextManagerInstance GetInstance(
            IEnumerable<UnityProblemAnalyzerContextSetting> settings)
        {
            return new UnityProblemAnalyzerContextManagerInstance(this, settings);
        }
    }

    public class UnityProblemAnalyzerContextManagerInstance
    {
        private readonly IReadOnlyList<IUnityProblemAnalyzerContextProvider> myProviders;

        public UnityProblemAnalyzerContextManagerInstance(UnityProblemAnalyzerContextManager manager,
            IEnumerable<UnityProblemAnalyzerContextSetting> settings)
        {
            var settingsList = settings.ToList();

            settingsList.AssertClassifications();

            var settingsDictionary = settingsList
                    .GroupBy(t => t.Context)
                    .ToDictionary(t => t.Key, 
                        t => t.ToList().First());

            myProviders = manager.Providers.Where(provider => settingsDictionary[provider.Context].IsAvailable)
                .ToList();
        }

        public UnityProblemAnalyzerContext CreateContext(UnityProblemAnalyzerContext context, ITreeNode node, DaemonProcessKind processKind)
        {
            if (!UnityCallGraphUtil.IsFunctionNode(node))
                return context;
            
            var newContext = UnityProblemAnalyzerContextElement.NONE;

            foreach (var provider in myProviders)
            {
                var providedContext = provider.CheckContext(node, processKind);

                if (providedContext == UnityProblemAnalyzerContextElement.NONE)
                    continue;

                newContext |= providedContext;
            }

            return context.Chain(newContext, node);
        }
    }
}