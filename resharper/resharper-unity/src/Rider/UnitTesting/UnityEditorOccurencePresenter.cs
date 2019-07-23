using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [OccurrencePresenter(Priority = 10.0)]
    public class UnityEditorOccurencePresenter : RangeOccurrencePresenter
    {
        public override bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityEditorOccurrence;
        }

        protected override RichText GetDisplayText(OccurrencePresentationOptions options, RangeOccurrence rangeOccurrence)
        {
            var processor = rangeOccurrence.GetSolution().NotNull("rangeOccurrence.GetSolution() != null")
                .GetComponent<UnitySceneDataLocalCache>();
            var occurrence = (rangeOccurrence as UnityEditorOccurrence).NotNull("rangeOccurrence as UnityEditorOccurrence != null");
            var reference = (occurrence.PrimaryReference as IUnityYamlReference).NotNull("occurrence.PrimaryReference as IUnityYamlReference != null");


            return GetAttachedGameObjectName(processor, reference.ComponentDocument);
        }
        
        public override bool Present(
            IMenuItemDescriptor descriptor,
            IOccurrence occurrence,
            OccurrencePresentationOptions options)
        {
            var unityOccurrence = (occurrence as UnityEditorOccurrence).NotNull("occurrence as UnityEditorOccurrence != null");

            var displayText = GetDisplayText(options, unityOccurrence) + OccurrencePresentationUtil.TextContainerDelimiter;
            descriptor.Text = displayText;
            OccurrencePresentationUtil.AppendRelatedFile(descriptor, unityOccurrence.SourceFile.DisplayName.Replace('\\', '/'));
            
            descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
            return true;
        }
        
        public static string GetAttachedGameObjectName(UnitySceneDataLocalCache cache, IYamlDocument document) {
    
            var consumer = new UnityPathCachedSceneConsumer();
            cache.ProcessSceneHierarchyFromComponentToRoot(document, consumer);

            var parts = consumer.NameParts;
            if (parts.Count == 0)
                return "...";
            return string.Join("/", consumer.NameParts);
        }
    }
}