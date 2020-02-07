using System.Text;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityAssetOccurrence : IOccurrence
    {
        public IPsiSourceFile SourceFile { get; }
        public TextRange TextRange { get; }
        public IHierarchyElement Parent { get; }

        public UnityAssetOccurrence(IPsiSourceFile sourceFile, TextRange textRange, IHierarchyElement parent)
        {
            SourceFile = sourceFile;
            TextRange = textRange;
            Parent = parent;
            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
        }

        public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            // if (!solution.GetComponent<ConnectionTracker>().IsConnectionEstablished())
            //     return base.Navigate(solution, windowContext, transferFocus, tabOptions);
            //
            // var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            // var reference = PrimaryReference as IUnityYamlReference;
            // if (reference == null)
            //     return true;
            // findRequestCreator.CreateRequestToUnity(reference, true);
            return false;
        }

        public ISolution GetSolution()
        {
            return SourceFile.GetSolution();
        }

        public string DumpToString()
        {
            var sb = new StringBuilder();
            sb.Append($"SourceFilePtr: = {SourceFile.PsiStorage.PersistentIndex}");
            sb.Append($"TextRange: = {TextRange}");
            return sb.ToString();
        }

        public OccurrenceType OccurrenceType => OccurrenceType.TextualOccurrence;
        public bool IsValid => SourceFile.IsValid();
        public OccurrencePresentationOptions PresentationOptions { get; set; }
    }
}