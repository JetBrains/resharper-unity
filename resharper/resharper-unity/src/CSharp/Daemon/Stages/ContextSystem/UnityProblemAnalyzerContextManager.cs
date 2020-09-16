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
        private readonly EmptyUnityProblemAnalyzerContextProvider myEmptyUnityProblemAnalyzerContextProvider;
        private readonly List<IUnityProblemAnalyzerContextProvider> myProviders;

        public UnityProblemAnalyzerContextManager(IEnumerable<IUnityProblemAnalyzerContextProvider> providers, EmptyUnityProblemAnalyzerContextProvider emptyUnityProblemAnalyzerContextProvider)
        {
            myEmptyUnityProblemAnalyzerContextProvider = emptyUnityProblemAnalyzerContextProvider;
            myProviders = providers.Where(provider => provider.IsEnabled).ToList();

            myProviders.AssertClassifications();
        }

        public UnityProblemAnalyzerContextManagerInstance GetInstance(
            IEnumerable<UnityProblemAnalyzerContextSetting> settings)
        {
            return new UnityProblemAnalyzerContextManagerInstance(myProviders, settings);
        }

        public IUnityProblemAnalyzerContextProvider GetContextProvider(UnityProblemAnalyzerContextSetting setting)
        {
            if (setting.IsAvailable == false)
                return myEmptyUnityProblemAnalyzerContextProvider;

            foreach (var contextProvider in myProviders)
            {
                if (contextProvider.Context == setting.Context)
                    return contextProvider;
            }

            throw new KeyNotFoundException($"No such context: {setting.Context}");
        }
    }

    public class UnityProblemAnalyzerContextManagerInstance
    {
        private readonly List<IUnityProblemAnalyzerContextProvider> myProviders;

        public UnityProblemAnalyzerContextManagerInstance(
            IEnumerable<IUnityProblemAnalyzerContextProvider> contextProviders,
            IEnumerable<UnityProblemAnalyzerContextSetting> settings)
        {
            var settingsList = settings.ToList();

            settingsList.AssertClassifications();

            var settingsDictionary = settingsList
                .GroupBy(t => t.Context)
                .ToDictionary(t => t.Key,
                    t => t.ToList().First());

            myProviders = contextProviders
                .Where(provider => settingsDictionary[provider.Context].IsAvailable)
                .ToList();
        }

        public UnityProblemAnalyzerContext CreateContext(UnityProblemAnalyzerContext context, ITreeNode node,
            DaemonProcessKind processKind)
        {
            if (!UnityCallGraphUtil.IsContextChangingNode(node))
                return context;

            var newContext = UnityProblemAnalyzerContextElement.NONE;

            foreach (var provider in myProviders)
            {
                var providedContext = provider.GetContext(node, processKind, false);

                if (providedContext == UnityProblemAnalyzerContextElement.NONE)
                    continue;

                newContext |= providedContext;
            }

            return context.Chain(newContext, node);
        }
    }
}