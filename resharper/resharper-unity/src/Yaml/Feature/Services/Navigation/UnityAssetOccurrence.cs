using System.Text;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public abstract class UnityAssetOccurrence : IOccurrence
    {
        public IPsiSourceFile SourceFile { get; }
        public IHierarchyElement AttachedElement { get; }
        public IDeclaredElementPointer<IDeclaredElement> DeclaredElementPointer { get; }

        protected UnityAssetOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement)
        {
            SourceFile = sourceFile;
            AttachedElement = attachedElement;
            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
            DeclaredElementPointer = declaredElement;
        }

        public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            return solution.GetComponent<UnityAssetOccurrenceNavigator>().Navigate(solution, DeclaredElementPointer, AttachedElement);
        }

        public ISolution GetSolution()
        {
            return SourceFile.GetSolution();
        }

        public string DumpToString()
        {
            var sb = new StringBuilder();
            sb.Append($"SourceFilePtr: = {SourceFile.PsiStorage.PersistentIndex}");
            return sb.ToString();
        }

        public OccurrenceType OccurrenceType => OccurrenceType.TextualOccurrence;
        public bool IsValid => SourceFile.IsValid();
        public OccurrencePresentationOptions PresentationOptions { get; set; }

        public override string ToString()
        {
            return $"Component usage ({AttachedElement.Location.LocalDocumentAnchor})";
        }
    }
}