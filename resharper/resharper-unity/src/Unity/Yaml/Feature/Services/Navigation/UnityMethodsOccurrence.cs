using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityEventHandlerOccurrence : UnityAssetOccurrence, IAssetOccurrenceWithTextOccurrence
    {
        public readonly AssetMethodUsages MethodUsages;

        public UnityEventHandlerOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement,
            LocalReference owningElementLocation, AssetMethodUsages methodUsages, bool isPrefabModification)
            : base(sourceFile, declaredElement, owningElementLocation, isPrefabModification)
        {
            MethodUsages = methodUsages;
        }


        public override string ToString()
        {
            return $"m_MethodName: {MethodUsages.MethodName}";
        }

        public override IconId GetIcon()
        {
            if (IsPrefabModification)
                return UnityFileTypeThemedIcons.FileUnityPrefab.Id;

            return base.GetIcon();
        }

        public IPsiSourceFile GetSourceFile()
        {
            return SourceFile;
        }

        public TextRange RenameTextRange => MethodUsages.TextRangeOwnerPsiPersistentIndex;
    }
}