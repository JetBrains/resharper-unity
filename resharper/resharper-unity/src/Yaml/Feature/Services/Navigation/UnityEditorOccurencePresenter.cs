using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
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
            var processor = occurrence.GetSolution().NotNull("occurrence.GetSolution() != null").GetComponent<AssetHierarchyProcessor>();
            var name =  GetAttachedGameObjectName(processor, occurrence);

            if (occurrence is UnityInspectorValuesOccurrence inspectorValuesOccurrence)
            {
                var solution = occurrence.GetSolution();
                return $"{inspectorValuesOccurrence.InspectorVariableUsage.Value.GetPresentation(solution, occurrence.DeclaredElementPointer.FindDeclaredElement(), true)} in {name}";
            }
            return name;
        }

        private string GetAttachedGameObjectName(AssetHierarchyProcessor processor, UnityAssetOccurrence occurrence)
        {
            return occurrence.GetSolution()?.GetComponent<DeferredCachesLocks>().ExecuteUnderReadLock(_ =>
            {
                var consumer = new UnityScenePathGameObjectConsumer();
                processor.ProcessSceneHierarchyFromComponentToRoot(occurrence.AttachedElementLocation, consumer, true, true);

                var parts = consumer.NameParts;
                if (parts.Count == 0)
                    return "...";
                return string.Join("/", consumer.NameParts);
            }) ?? "UNKNOWN";
        }
    }
}