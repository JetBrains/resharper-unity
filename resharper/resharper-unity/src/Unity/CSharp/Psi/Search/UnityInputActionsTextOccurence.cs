using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search
{
    public class UnityInputActionsTextOccurence : TextOccurrence
    {
        public UnityInputActionsTextOccurence(IPsiSourceFile sourceFile, DocumentRange documentRange,
            OccurrencePresentationOptions presentationOptions,
            OccurrenceType occurrenceType = OccurrenceType.Occurrence) : base(sourceFile, documentRange,
            presentationOptions, occurrenceType)
        {
        }

        [CanBeNull]
        public virtual string GetRelatedFolderPresentation()
        {
            var parts = SourceFile.DisplayName.Split('\\').ToArray();
            if (parts.Length == 1)
                return null;
            
            var path = string.Join("/", parts.Take(parts.Length - 1));
            return path;
        }

        public virtual IconId GetIcon()
        {
            return UnityFileTypeThemedIcons.UsageInputActions.Id;
        }
    }
}