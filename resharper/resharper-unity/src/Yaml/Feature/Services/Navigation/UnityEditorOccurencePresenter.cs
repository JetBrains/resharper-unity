using System.Drawing;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
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
            var declaredElement = unityOccurrence.DeclaredElementPointer.FindDeclaredElement();
            if (declaredElement == null)
                return false;
                
            var displayText = unityOccurrence.GetDisplayText() + OccurrencePresentationUtil.TextContainerDelimiter;
            descriptor.Text = displayText;
            
            
            AppendRelatedFile(descriptor, unityOccurrence.GetRelatedFilePresentation());
            
            descriptor.Icon = UnityFileTypeThemedIcons.FileUnity.Id;
            return true;
        }
        
        public static void AppendRelatedFile(IMenuItemDescriptor descriptor, string relatedFilePresentation)
        {
            descriptor.ShortcutText =  new RichText($"in {relatedFilePresentation}", TextStyle.FromForeColor(Color.DarkGray));
        }

        
        public bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityAssetOccurrence;
        }
    }
}