using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [PsiComponent]
    public class UnityEventHandlerReferenceCache : SimpleICache<List<string>>
    {
        private readonly CompactOneToListMap<string, IPsiSourceFile> myReferencedElementToAsset =
            new CompactOneToListMap<string, IPsiSourceFile>();

        public UnityEventHandlerReferenceCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, CreateMarshaller())
        {
#if DEBUG
            ClearOnLoad = true;
#endif
        }

        private static IUnsafeMarshaller<List<string>> CreateMarshaller()
        {
            return UnsafeMarshallers.GetCollectionMarshaller(UnsafeMarshallers.UnicodeStringMarshaller,
                n => new List<string>(n));
        }

        public bool IsEventHandler([NotNull] IDeclaredElement declaredElement)
        {
            var sourceFiles = declaredElement.GetSourceFiles();
            if (sourceFiles.Count != 1)
                return false;

            var referencedElementKey = GetReferencedElementKey(sourceFiles[0], declaredElement);
            return myReferencedElementToAsset[referencedElementKey].Count > 0;
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<YamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   UnityYamlFileExtensions.IsAsset(sourceFile.GetLocation());
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>();
            if (file == null)
                return null;

            var referencedElements = new List<string>();
            var referenceProcessor = new RecursiveReferenceProcessor<UnityEventTargetReference>(reference =>
            {
                var location = reference.GetSourceFileLocation();
                if (!location.IsEmpty)
                {
                    var referencedElementKey = GetReferencedElementKey(location, reference.EventHandlerName);
                    if (referencedElementKey != null)
                        referencedElements.Add(referencedElementKey);
                }
            });

            referenceProcessor.ProcessForResolve(file);

            return referencedElements.Count > 0 ? referencedElements : null;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            CleanLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);

            foreach (var referencedElement in (List<string>) builtPart ?? EmptyList<string>.InstanceList)
                myReferencedElementToAsset.AddValue(referencedElement, sourceFile);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            CleanLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void CleanLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var referencedElements))
            {
                foreach (var referencedElement in referencedElements)
                    myReferencedElementToAsset.RemoveValue(referencedElement, sourceFile);
            }
        }

        [CanBeNull]
        private string GetReferencedElementKey(IPsiSourceFile sourceFile, IDeclaredElement declaredElement)
        {
            switch (declaredElement)
            {
                case IMethod method:
                    return GetReferencedElementKey(sourceFile.GetLocation(), method.ShortName);

                case IProperty property:
                    return GetReferencedElementKey(sourceFile.GetLocation(), property.Setter?.ShortName);
            }

            return null;
        }

        private string GetReferencedElementKey(FileSystemPath location, [CanBeNull] string handlerName)
        {
            if (handlerName == null)
                return null;

            return location + "::" + handlerName;
        }
    }
}