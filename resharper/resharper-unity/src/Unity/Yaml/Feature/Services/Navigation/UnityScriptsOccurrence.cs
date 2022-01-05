using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityScriptsOccurrence : UnityAssetOccurrence
    {
        private readonly Guid myGuid;

        public UnityScriptsOccurrence(IPsiSourceFile sourceFile,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, LocalReference owningElementLocation, Guid guid)
            : base(sourceFile, declaredElement, owningElementLocation, false)
        {
            myGuid = guid;
        }

        public override RichText GetDisplayText()
        {
            var declaredElement = DeclaredElementPointer.FindDeclaredElement();
            if (declaredElement == null)
                return base.GetDisplayText();

            if ((declaredElement as IClass).DerivesFromScriptableObject())
                return SourceFile.GetLocation().Name;

            return base.GetDisplayText();
        }

        public override string GetRelatedFilePresentation()
        {
            if (IsRelatedToScriptableObject())
                return null;
            return base.GetRelatedFilePresentation();
        }

        private bool IsRelatedToScriptableObject()
        {
            return (DeclaredElementPointer.FindDeclaredElement() as IClass).DerivesFromScriptableObject();
        }

        public override string ToString()
        {
            return $"Guid: {myGuid:N}";
        }
    }
}