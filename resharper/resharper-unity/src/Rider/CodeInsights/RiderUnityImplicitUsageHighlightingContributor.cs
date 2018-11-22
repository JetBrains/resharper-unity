using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Features.Icons;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class RiderUnityImplicitUsageHighlightingContributor : UnityImplicitUsageHighlightingContributor
    {
        private readonly UnityImplicitFieldUsageProvider myFieldUsageProvider;
        private readonly UnityImplicitCodeInsightProvider myImplicitCodeInsightProvider;
        private readonly IconHost myIconHost;

        public RiderUnityImplicitUsageHighlightingContributor(ISolution solution, ITextControlManager textControlManager,
            UnityImplicitFieldUsageProvider fieldUsageProvider,  UnityImplicitCodeInsightProvider implicitCodeInsightProvider,
            ISettingsStore settingsStore, IconHost iconHost = null)
            : base(solution, settingsStore, textControlManager)
        {
            myFieldUsageProvider = fieldUsageProvider;
            myImplicitCodeInsightProvider = implicitCodeInsightProvider;
            myIconHost = iconHost;
        }
        
        public override void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element, string tooltip)
        {
            switch (element)
            {
                case IFieldDeclaration _:
                    AddHighlighting(consumer, myFieldUsageProvider, element, tooltip, "Set by Unity");
                    break;
                case IClassLikeDeclaration _:
                    AddHighlighting(consumer, myImplicitCodeInsightProvider, element, tooltip, "Scripting component");
                    break;
                case IMethodDeclaration _:
                    AddHighlighting(consumer, myImplicitCodeInsightProvider, element, tooltip, "Event function");
                    break;
                default:
                    AddHighlighting(consumer, myImplicitCodeInsightProvider, element, tooltip, "Implicit usage");
                    break;
            }
        }
        
        private void AddHighlighting(IHighlightingConsumer consumer, AbstractUnityImplicitProvider codeInsightsProvider, ICSharpDeclaration element, string tooltip, string displayName)
        {
            if (SettingsStore.GetIndexedValue((CodeInsightsSettings key) => key.DisabledProviders, codeInsightsProvider.ProviderId))
            {
                base.AddHighlighting(consumer, element, tooltip);
                return;
            }

            if (SettingsStore.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.Always)
            {
                base.AddHighlighting(consumer, element, tooltip);
            }
            
            displayName = displayName ?? codeInsightsProvider.DisplayName;
            var declaredElement = element.DeclaredElement;
            if (declaredElement == null || !declaredElement.IsValid())
                return;
            
            consumer.AddHighlighting(new UnityCodeInsightsHighlighting(element.NameIdentifier.GetDocumentRange(),
                displayName, displayName, codeInsightsProvider, declaredElement, myIconHost.Transform(codeInsightsProvider.IconId), CreateBulbItemsForUnityDeclaration(element)));
        }

    }
}