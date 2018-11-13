using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.CodeInsights;
using JetBrains.ReSharper.Host.Features.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Lenses;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class RiderUnityImplicitUsageHighlightingContributor : UnityImplicitUsageHighlightingContributor
    {
        private readonly UnityImplicitFieldUsageProvider myFieldUsageProvider;
        private readonly UnityImplicitCodeInsightProvider myImplicitCodeInsightProvider;
        private readonly IconHost myIconHost;

        public RiderUnityImplicitUsageHighlightingContributor(ISolution solution, ITextControlManager textControlManager,
            UnityImplicitFieldUsageProvider fieldUsageProvider,  UnityImplicitCodeInsightProvider implicitCodeInsightProvider,
            ISettingsStore settingsStore, IconHost iconHost)
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
                    AddHighlighting(consumer, myFieldUsageProvider, element, tooltip, null);
                    break;
                case IClassLikeDeclaration _:
                    AddHighlighting(consumer, myImplicitCodeInsightProvider, element, tooltip, "Implicit usage");
                    break;
                default:
                    AddHighlighting(consumer, myImplicitCodeInsightProvider, element, tooltip, null);
                    break;
            }
           
        }
        
        private void AddHighlighting(IHighlightingConsumer consumer, AbstractUnityImplicitProvider codeInsightsProvider, ICSharpDeclaration element, string tooltip, string displayName)
        {
            if (SettingsStore.GetIndexedValue((CodeInsightsSettings key) => key.DisabledProviders, codeInsightsProvider.ProviderId) ||
                SettingsStore.GetValue((UnitySettings key) => key.UnityHighlighterSchemeKind) != UnityHighlighterSchemeKind.CodeInsights)
            {
                base.AddHighlighting(consumer, element, tooltip);
                return;
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