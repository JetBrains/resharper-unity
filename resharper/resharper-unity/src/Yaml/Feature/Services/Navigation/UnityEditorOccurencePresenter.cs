using System;
using System.Drawing;
using System.Text;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
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
            
            
            AppendRelatedFile(descriptor, unityOccurrence.GetRelatedFilePresentation(), unityOccurrence.GetRelatedFolderPresentation());

            descriptor.Icon = unityOccurrence.GetIcon();
            return true;
        }
        
        public static void AppendRelatedFile(IMenuItemDescriptor descriptor, string relatedFilePresentation, string relatedFolderPresentation)
        {
            var sb = new StringBuilder();
            if (relatedFilePresentation != null)
                sb.Append($"{relatedFilePresentation}");
            
            if (relatedFolderPresentation != null)
            {
                if (relatedFilePresentation != null)
                    sb.Append(" ");
                
                sb.Append($"in {relatedFolderPresentation}");
            }
            
            descriptor.ShortcutText = new RichText(sb.ToString(), TextStyle.FromForeColor(Color.DarkGray));
        }

        
        public bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is UnityAssetOccurrence;
        }
    }
}