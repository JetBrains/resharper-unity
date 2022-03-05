using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Diagnostics;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
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
        public readonly IPsiSourceFile SourceFile;
        public readonly IDeclaredElementPointer<IDeclaredElement> DeclaredElementPointer;

        public readonly LocalReference OwningElementLocation;
        protected readonly bool IsPrefabModification;
        

        protected UnityAssetOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, LocalReference owningElementLocation, bool isPrefabModification)
        {
            SourceFile = sourceFile;
            OwningElementLocation = owningElementLocation;
            PresentationOptions = OccurrencePresentationOptions.DefaultOptions;
            DeclaredElementPointer = declaredElement;
            IsPrefabModification = isPrefabModification;
        }

        public virtual bool Navigate(ISolution solution, PopupWindowContextSource windowContext, bool transferFocus,
            TabOptions tabOptions = TabOptions.Default)
        {
            return solution.GetComponent<UnityAssetOccurrenceNavigator>().Navigate(solution, DeclaredElementPointer, OwningElementLocation);
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

        public OccurrenceType OccurrenceType => OccurrenceType.Occurrence;
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
            return $"Component (id = {OwningElementLocation.LocalDocumentAnchor})";
        }

        public virtual RichText GetDisplayText()
        {
            var processor = GetSolution().NotNull("occurrence.GetSolution() != null").GetComponent<AssetHierarchyProcessor>();
            var name = GetAttachedGameObjectName(processor);
            return name;
        }
        
        private string GetAttachedGameObjectName(AssetHierarchyProcessor processor)
        {
            if (SourceFile.IsController())
                return "AnimatorStateMachine";
            
            var consumer = new UnityScenePathGameObjectConsumer();
            processor.ProcessSceneHierarchyFromComponentToRoot(OwningElementLocation, consumer, true, true);

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