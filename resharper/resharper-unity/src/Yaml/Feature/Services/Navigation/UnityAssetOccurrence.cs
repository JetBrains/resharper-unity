using System.Text;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public abstract class UnityAssetOccurrence : IOccurrence
    {
        public IPsiSourceFile SourceFile { get; }
        public LocalReference AttachedElementLocation { get; }
        public IDeclaredElementPointer<IDeclaredElement> DeclaredElementPointer { get; }

        protected UnityAssetOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement)
        {
            SourceFile = sourceFile;
            AttachedElementLocation = attachedElement.Location;
            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
            DeclaredElementPointer = declaredElement;
        }

        public bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            return solution.GetComponent<UnityAssetOccurrenceNavigator>().Navigate(solution, DeclaredElementPointer, AttachedElementLocation);
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

        public virtual string GetRelatedFilePresentation()
        {
            return SourceFile.DisplayName.Replace('\\', '/');
        }

        public override string ToString()
        {
            return $"Component (id = {AttachedElementLocation.LocalDocumentAnchor})";
        }

        protected bool Equals(UnityAssetOccurrence other)
        {
            return SourceFile.Equals(other.SourceFile) && AttachedElementLocation.Equals(other.AttachedElementLocation);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityAssetOccurrence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SourceFile.GetHashCode() * 397) ^ AttachedElementLocation.GetHashCode();
            }
        }

        public virtual string GetDisplayText()
        {
            var processor = GetSolution().NotNull("occurrence.GetSolution() != null").GetComponent<AssetHierarchyProcessor>();
            var name = GetAttachedGameObjectName(processor);
            return name;
        }
        
        private string GetAttachedGameObjectName(AssetHierarchyProcessor processor)
        {
            var consumer = new UnityScenePathGameObjectConsumer();
            processor.ProcessSceneHierarchyFromComponentToRoot(AttachedElementLocation, consumer, true, true);

            var parts = consumer.NameParts;
            if (parts.Count == 0)
                return "...";
            return string.Join("/", consumer.NameParts);
        }
    }
}