using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Platform.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstCodeVisionProvider : BurstProblemAnalyzerBase<ICSharpFunctionDeclaration>
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly UnityCodeInsightProvider myCodeInsightProvider;
        private readonly IconHost myIconHost;
     
        public BurstCodeVisionProvider(ISolution solution,
            SettingsStore settingsStore,
            UnityCodeInsightProvider codeInsightProvider, IconHost iconHost)
        {
            mySettingsStore = settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
            myCodeInsightProvider = codeInsightProvider;
            myIconHost = iconHost;
        }

        protected override bool CheckAndAnalyze(ICSharpFunctionDeclaration t, IHighlightingConsumer consumer)
        {
            if(consumer == null)
                return false;
            const string BurstText = "BURST_TEST";

            if (RiderIconProviderUtil.IsCodeVisionEnabled(mySettingsStore, myCodeInsightProvider.ProviderId,
                () => { }, out var useFallback))
            {
                var declaredElement = t.DeclaredElement;
                AddBurstHighlighting(consumer, t, BurstText, EnumerableCollection<BulbMenuItem>.Empty);
                myCodeInsightProvider.AddHighlighting(consumer, t, declaredElement, BurstText, BurstText, BurstText,
                    myIconHost.Transform(InsightUnityIcons.InsightUnity.Id), EnumerableCollection<BulbMenuItem>.Empty, null);
            }

            return false;
        }

        private static void AddBurstHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            string tooltip, IEnumerable<BulbMenuItem> items)
        {
            var highlighting = (IHighlighting) new UnityGutterMarkInfo(items, element, tooltip);
            consumer.AddHighlighting(highlighting);
        }
        
    }
}