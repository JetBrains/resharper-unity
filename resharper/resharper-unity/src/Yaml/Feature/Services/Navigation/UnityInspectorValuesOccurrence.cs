using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Resources.Shell;

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
                    var value = InspectorVariableUsage.Value.GetPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement(), true);
                    return $"{InspectorVariableUsage.Name} = {value}";
                }
            }
        }
    }
}