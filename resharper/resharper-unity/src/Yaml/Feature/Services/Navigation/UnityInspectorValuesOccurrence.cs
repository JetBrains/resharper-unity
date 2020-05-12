using System.Drawing;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.RichText;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityInspectorValuesOccurrence: UnityAssetOccurrence
    {
        public InspectorVariableUsage InspectorVariableUsage { get; }

        public UnityInspectorValuesOccurrence(IPsiSourceFile sourceFile, InspectorVariableUsage inspectorVariableUsage,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement)
            : base(sourceFile, declaredElement, attachedElement)
        {
            InspectorVariableUsage = inspectorVariableUsage;
        }

        public override RichText GetDisplayText()
        {
            var declaredElement = DeclaredElementPointer.FindDeclaredElement();
            if (declaredElement == null)
                return base.GetDisplayText();
            
            var solution = GetSolution();
            var valuePresentation = InspectorVariableUsage.Value.GetPresentation(solution, declaredElement , true);
            if (SourceFile.GetLocation().IsAsset())
                return valuePresentation;
            
            var richText = new RichText(valuePresentation);
            var inText = new RichText(" in ", TextStyle.FromForeColor(Color.DarkGray));
            var objectText = base.GetDisplayText();

            return richText.Append(inText).Append(objectText);
        }

        private bool IsRelatedToScriptableObject() => UnityApi.IsDescendantOfScriptableObject((DeclaredElementPointer.FindDeclaredElement() as IField)?.Type.GetTypeElement());
        
        protected bool Equals(UnityInspectorValuesOccurrence other)
        {
            return base.Equals(other) && InspectorVariableUsage.Equals(other.InspectorVariableUsage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityInspectorValuesOccurrence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ InspectorVariableUsage.GetHashCode();
            }
        }

        public override string ToString()
        {
            using (ReadLockCookie.Create())
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                {
                    if (IsRelatedToScriptableObject())
                    {
                        var value = InspectorVariableUsage.Value.GetFullPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement(), true);
                        return $"{InspectorVariableUsage.Name} = {value}";
                    }
                    else
                    {
                        var value = InspectorVariableUsage.Value.GetPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement(), true);
                        return $"{InspectorVariableUsage.Name} = {value}";
                    }
                }
            }
        }
    }
}