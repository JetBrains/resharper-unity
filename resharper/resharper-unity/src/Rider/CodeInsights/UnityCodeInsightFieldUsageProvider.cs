using System.Collections.Generic;
using JetBrains.Application.UI.Controls;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightFieldUsageProvider : AbstractUnityCodeInsightProvider
    {
        public override string ProviderId => "Unity serialized field";
        public override string DisplayName => "Unity serialized field";
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Right;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>
            new[] {new CodeLensRelativeOrderingLast()};

        public UnityCodeInsightFieldUsageProvider(UnitySolutionTracker unitySolutionTracker, UnityHost host,
            BulbMenuComponent bulbMenu)
            : base(unitySolutionTracker, host, bulbMenu)
        {
        }
        
        private static (string guid, string propertyName)? GetAssetGuidAndPropertyName(ISolution solution, IDeclaredElement declaredElement)
        {
            var containingType = (declaredElement as IClrDeclaredElement)?.GetContainingType();
            if (containingType == null)
                return null;

            var sourceFile = declaredElement.GetSourceFiles().FirstOrDefault();
            if (sourceFile == null)
                return null;

            var guid = solution.GetComponent<MetaFileGuidCache>().GetAssetGuid(sourceFile);
            if (guid == null)
                return null;

            return (guid, declaredElement.ShortName);
        }
        
        public override void OnClick(CodeInsightsHighlighting highlighting, ISolution solution)
        {
            if (highlighting is UnityInspectorCodeInsightsHighlighting)
            {
                Shell.Instance.GetComponent<JetPopupMenus>().Show(solution.GetLifetime(),
                    JetPopupMenu.ShowWhen.NoItemsBannerIfNoItems, (lifetime, menu) =>
                    {

                        var result = GetAssetGuidAndPropertyName(solution, highlighting.DeclaredElement);
                        if (!result.HasValue)
                            return;

                        var valuesCache = solution.GetComponent<UnityPropertyValueCache>();
                        var namesCache = solution.GetComponent<UnityGameObjectNamesCache>();
                        var values = valuesCache.GetUnityPropertyValues(result.Value.guid, result.Value.propertyName);

                        menu.Caption.Value = WindowlessControlAutomation.Create("Unity Editor values");
                        menu.KeyboardAcceleration.Value = KeyboardAccelerationFlags.QuickSearch;

                        menu.ItemKeys.AddRange(values);



                        menu.DescribeItem.Advise(lifetime, e =>
                        {
                            var value = (e.Key as UnityPropertyValueCache.MonoBehaviourPropertyValueWithLocation)
                                .NotNull("value != null");

                            var shortRepresentation = value.GetSimplePresentation(solution) ?? "???";

                            e.Descriptor.Text = shortRepresentation;
                            OccurrencePresentationUtil.AppendRelatedFile(e.Descriptor, value.File.DisplayName);

                            e.Descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
                        });

                        menu.ItemClicked.Advise(lifetime, key => { });
                    });
            }
        }

        public void AddInspectorHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            IDeclaredElement declaredElement, IconModel iconModel)
        {
            string displayName;
            string tooltip = "Values from Unity Editor Inspector";

            var solution = element.GetSolution();

            var result = GetAssetGuidAndPropertyName(solution, declaredElement);
            if (!result.HasValue)
                return;

            var cache = solution.GetComponent<UnityPropertyValueCache>();
            var values = cache.GetUnityPropertyValues(result.Value.guid, result.Value.propertyName);
            if (values.Count == 0)
            {
                displayName = "No Inspector values";
            } else if (values.Count == 1)
            {
                var valueWithLocation = values[0];
                var presentation = valueWithLocation.GetSimplePresentation(solution);
                if (presentation != null)
                {
                    displayName = $"{presentation}";
                }
                else
                {
                    displayName = "1 Inspector value";
                }
            }
            else
            {
                displayName = $"{values.Count} Inspector values";
            }
            
            consumer.AddHighlighting(new UnityInspectorCodeInsightsHighlighting(element.GetNameDocumentRange(), displayName, tooltip, "Property Inspector values",
                this, declaredElement, iconModel));
        }
    }
}