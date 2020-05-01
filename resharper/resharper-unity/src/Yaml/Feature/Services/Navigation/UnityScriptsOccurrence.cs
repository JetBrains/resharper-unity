using System;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.UI.RichText;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityScriptsOccurrence : UnityAssetOccurrence
    {
        private readonly Guid myGuid;

        public UnityScriptsOccurrence(IPsiSourceFile sourceFile,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, LocalReference attachedElementLocation, Guid guid)
            : base(sourceFile, declaredElement, attachedElementLocation)
        {
            myGuid = guid;
        }

        public override RichText GetDisplayText()
        {
            var declaredElement = DeclaredElementPointer.FindDeclaredElement();
            if (declaredElement == null)
                return base.GetDisplayText();
            
            if (UnityApi.IsDescendantOfScriptableObject(declaredElement as IClass))
                return SourceFile.GetLocation().Name;  
            
            return base.GetDisplayText();
        }

        public override string GetRelatedFilePresentation()
        {
            if (IsRelatedToScriptableObject())
                return null;
            return base.GetRelatedFilePresentation();
        }
        
        private bool IsRelatedToScriptableObject() => UnityApi.IsDescendantOfScriptableObject(DeclaredElementPointer.FindDeclaredElement() as IClass);
        
        public override string ToString()
        {
            return $"Guid: {myGuid:N}";
        }
    }
}