using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public abstract class UnityAssetOccurrence : IOccurrence
    {
        public IPsiSourceFile SourceFile { get; }
        public LocalReference AttachedElementLocation { get; }
        public IDeclaredElementPointer<IDeclaredElement> DeclaredElementPointer { get; }

        protected UnityAssetOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, LocalReference attachedElementLocation)
        {
            SourceFile = sourceFile;
            AttachedElementLocation = attachedElementLocation;
            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
            DeclaredElementPointer = declaredElement;
        }

        public virtual bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
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

        [CanBeNull]
        public virtual string GetRelatedFilePresentation()
        {
            return SourceFile.DisplayName.Split('\\').Last();
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
        
        public override string ToString()
        {
            return $"Component (id = {AttachedElementLocation.LocalDocumentAnchor})";
        }

        public virtual RichText GetDisplayText()
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

        public virtual IconId GetIcon()
        {
            return UnityFileTypeThemedIcons.FileUnity.Id;
        }
    }
}