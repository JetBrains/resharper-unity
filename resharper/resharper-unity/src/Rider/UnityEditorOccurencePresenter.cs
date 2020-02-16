using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OccurrencePresenter(Priority = 10.0)]
    public class UnityEditorOccurencePresenter : IOccurrencePresenter
    {
        public bool Present(IMenuItemDescriptor descriptor, IOccurrence occurrence,
            OccurrencePresentationOptions occurrencePresentationOptions)
        {
            var unityOccurrence = (occurrence as UnityAssetOccurrence).NotNull("occurrence as UnityAssetOccurrence != null");
            
            var displayText = GetDisplayText(unityOccurrence) + OccurrencePresentationUtil.TextContainerDelimiter;
            descriptor.Text = displayText;
            OccurrencePresentationUtil.AppendRelatedFile(descriptor, unityOccurrence.SourceFile.DisplayName.Replace('\\', '/'));
            
            descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
            return true;
        }

        public bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityAssetOccurrence;
        }

        protected RichText GetDisplayText(UnityAssetOccurrence occurrence)
        {
            // var processor = occurrence.GetSolution().NotNull("rangeOccurrence.GetSolution() != null")
            //     .GetComponent<UnitySceneDataLocalCache>();
            // var occurrence = (rangeOccurrence as UnityEditorOccurrence).NotNull("rangeOccurrence as UnityEditorOccurrence != null");
            // var reference = (occurrence.PrimaryReference as IUnityYamlReference).NotNull("occurrence.PrimaryReference as IUnityYamlReference != null");
        
        
            // return GetAttachedGameObjectName(processor, reference.ComponentDocument);

            return occurrence.Parent.Location.LocalDocumentAnchor;
        }

        //
        // public static string GetAttachedGameObjectName(UnitySceneDataLocalCache cache, IYamlDocument document) {
        //
        //     var consumer = new UnityPathCachedSceneConsumer();
        //     cache.ProcessSceneHierarchyFromComponentToRoot(document, consumer);
        //
        //     var parts = consumer.NameParts;
        //     if (parts.Count == 0)
        //         return "...";
        //     return string.Join("/", consumer.NameParts);
        // }
    }
}