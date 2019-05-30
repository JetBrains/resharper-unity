using System.Drawing;
using System.Linq;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
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

            // false to show full scene path. Very expensive
            var unityPathSceneConsumer = new UnityPathSceneConsumer(true);
            var processor = rangeOccurrence.GetSolution().NotNull("rangeOccurrence.GetSolution() != null")
                .GetComponent<UnitySceneProcessor>();
            var occurrence = (rangeOccurrence as UnityEditorOccurrence).NotNull("rangeOccurrence as UnityEditorOccurrence != null");
            var reference = (occurrence.PrimaryReference as IUnityYamlReference).NotNull("occurrence.PrimaryReference as IUnityYamlReference != null");
            processor.ProcessSceneHierarchyFromComponentToRoot(reference.ComponentDocument, unityPathSceneConsumer);
                
            
            return unityPathSceneConsumer.NameParts.FirstOrDefault() ?? "Unknown";
        }
        
        public override bool Present(
            IMenuItemDescriptor descriptor,
            IOccurrence occurrence,
            OccurrencePresentationOptions options)
        {
            var unityOccurrence = (occurrence as UnityEditorOccurrence).NotNull("occurrence as UnityEditorOccurrence != null");

            var displayText = GetDisplayText(options, unityOccurrence) + OccurrencePresentationUtil.TextContainerDelimiter;
            descriptor.Text = displayText;
            OccurrencePresentationUtil.AppendRelatedFile(descriptor, unityOccurrence.SourceFile.DisplayName);
            
            descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
            return true;
        }
    }
}