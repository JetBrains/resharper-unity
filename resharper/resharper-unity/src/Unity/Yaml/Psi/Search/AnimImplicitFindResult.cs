using System.Text;
using JetBrains.Application.UI.Controls.JetPopupMenu;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Presentation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.UI.RichText;
using JetBrains.Util.Media;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    internal class AnimImplicitFindResult : FindResultText
    {
        public AnimImplicitFindResult(IPsiSourceFile sourceFile, DocumentRange documentRange) : base(
            sourceFile, documentRange)
        {
        }
    }
    
    
    [OccurrencePresenter(Priority = 10.0)]
    internal class AnimImplicitTextOccurencePresenter : RangeOccurrencePresenter
    {
        public override bool Present(IMenuItemDescriptor descriptor, IOccurrence occurrence,
            OccurrencePresentationOptions occurrencePresentationOptions)
        {
            var result = base.Present(descriptor, occurrence, occurrencePresentationOptions);
            var animImplicitOccurence = (occurrence as AnimImplicitOccurence).NotNull("occurrence as AnimImplicitOccurence != null");
            AppendRelatedFolder(descriptor, animImplicitOccurence.GetRelatedFolderPresentation());
            descriptor.Icon = animImplicitOccurence.GetIcon();
            return result;
        }
        
        public static void AppendRelatedFolder(IMenuItemDescriptor descriptor, string relatedFolderPresentation)
        {
            var sb = new StringBuilder();
            
            if (relatedFolderPresentation != null)
            {
                sb.Append($"in {relatedFolderPresentation}");
            }
            
            descriptor.ShortcutText = new RichText(sb.ToString(), TextStyle.FromForeColor(JetRgbaColors.DarkGray));
        }
        
        public override bool IsApplicable(IOccurrence occurrence)
        {
            return occurrence is AnimImplicitOccurence;
        }
    }
}