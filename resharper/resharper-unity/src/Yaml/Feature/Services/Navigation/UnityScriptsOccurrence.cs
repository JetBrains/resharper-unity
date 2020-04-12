using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityScriptsOccurrence : UnityAssetOccurrence
    {
        private readonly string myGuid;

        public UnityScriptsOccurrence(IPsiSourceFile sourceFile,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement, string guid)
            : base(sourceFile, declaredElement, attachedElement)
        {
            myGuid = guid;
        }

        public override string GetDisplayText()
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
                return base.GetRelatedFilePresentation().RemoveEnd("/" + SourceFile.GetLocation().Name).RemoveStart("Assets/");
            return base.GetRelatedFilePresentation();
        }

        private bool IsRelatedToScriptableObject() => UnityApi.IsDescendantOfScriptableObject(DeclaredElementPointer.FindDeclaredElement() as IClass);
        
        protected bool Equals(UnityScriptsOccurrence other)
        {
            return base.Equals(other) && myGuid == other.myGuid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityScriptsOccurrence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ myGuid.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"Guid: {myGuid}";
        }
    }
}