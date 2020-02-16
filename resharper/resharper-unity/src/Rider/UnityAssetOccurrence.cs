using System.Text;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public class UnityAssetOccurrence : IOccurrence
    {
        public IPsiSourceFile SourceFile { get; }
        public TextRange TextRange { get; }
        public IHierarchyElement Parent { get; }
        public IDeclaredElementPointer<IDeclaredElement> DeclaredElementPointer { get; }

        public UnityAssetOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, TextRange textRange, IHierarchyElement parent)
        {
            SourceFile = sourceFile;
            TextRange = textRange;
            Parent = parent;
            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
            DeclaredElementPointer = declaredElement;
        }

        public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            if (!solution.GetComponent<ConnectionTracker>().IsConnectionEstablished())
                return true;
            
            var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            var declaredElement = DeclaredElementPointer.FindDeclaredElement();
            if (declaredElement == null)
                return true;
            
            findRequestCreator.CreateRequestToUnity(declaredElement, Parent, true);
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