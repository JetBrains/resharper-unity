using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
        private readonly List<IUnityProblemAnalyzerContextChanger> myContextChangers;
        private readonly List<IUnityProblemAnalyzerContextProvider> myProviders;

        public UnityProblemAnalyzerContextManager(IEnumerable<IUnityProblemAnalyzerContextProvider> providers,
            EmptyUnityProblemAnalyzerContextProvider emptyUnityProblemAnalyzerContextProvider,
            IEnumerable<IUnityProblemAnalyzerContextChanger> contextChangers)
        {
            myEmptyUnityProblemAnalyzerContextProvider = emptyUnityProblemAnalyzerContextProvider;
            myContextChangers = contextChangers.ToList();
            myProviders = providers.Where(provider => provider.IsProblemContextBound).ToList();

            myProviders.AssertClassifications();
        }

        [NotNull]
        public UnityProblemAnalyzerContextManagerInstance GetInstance(
            [NotNull] List<UnityProblemAnalyzerContextSetting> settings)
        {
            return new UnityProblemAnalyzerContextManagerInstance(myProviders, settings, myContextChangers);
        }

        [NotNull]
        public IUnityProblemAnalyzerContextProvider GetContextProvider([NotNull] UnityProblemAnalyzerContextSetting setting)
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
        [NotNull] private readonly List<IUnityProblemAnalyzerContextChanger> myContextChangers;
        [NotNull] private readonly List<IUnityProblemAnalyzerContextProvider> myProviders;

        public UnityProblemAnalyzerContextManagerInstance(
            List<IUnityProblemAnalyzerContextProvider> contextProviders,
            List<UnityProblemAnalyzerContextSetting> settings,
            List<IUnityProblemAnalyzerContextChanger> contextChangers)
        {
            settings.AssertClassifications();

            var settingsDictionary = settings
                .GroupBy(t => t.Context)
                .ToDictionary(t => t.Key,
                    t => t.ToList().First());

            myProviders = contextProviders
                .Where(provider => settingsDictionary[provider.Context].IsAvailable)
                .ToList();


            myContextChangers = contextChangers
                .Where(changer => settingsDictionary[changer.Context].IsAvailable)
                .ToList();
        }

        [NotNull]
        public UnityProblemAnalyzerContext CreateContext([NotNull] UnityProblemAnalyzerContext context, [NotNull] ITreeNode node,
            DaemonProcessKind processKind)
        {
            var contextsToChange = UnityProblemAnalyzerContextElement.NONE;

            if (UnityCallGraphUtil.IsFunctionNode(node))
                contextsToChange = UnityProblemAnalyzerContextElementUtil.ALL;
            else
            {
                foreach (var contextChanger in myContextChangers)
                {
                    if (contextChanger.IsContextChangingNode(node))
                        contextsToChange |= contextChanger.Context;
                }
            }

            if (contextsToChange == UnityProblemAnalyzerContextElement.NONE)
                return context;

            var newContext = UnityProblemAnalyzerContextElement.NONE;

            foreach (var provider in myProviders)
            {
                if (contextsToChange.HasFlag(provider.Context))
                    newContext |= provider.GetContext(node, processKind, false);
                else if (provider.Context.HasFlag(provider.Context))
                    newContext |= provider.Context;
            }

            return context.Chain(newContext, node);
        }
    }
}