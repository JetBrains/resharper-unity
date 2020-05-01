using System.Drawing;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityInspectorValuesOccurrence: UnityAssetOccurrence
    {
        public InspectorVariableUsage InspectorVariableUsage { get; }
        public bool IsPrefabModification { get; }

        public UnityInspectorValuesOccurrence(IPsiSourceFile sourceFile, InspectorVariableUsage inspectorVariableUsage,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, LocalReference attachedElementLocation, bool isPrefabModification)
            : base(sourceFile, declaredElement, attachedElementLocation)
        {
            InspectorVariableUsage = inspectorVariableUsage;
            IsPrefabModification = isPrefabModification;
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

        public override string ToString()
        {
            using (ReadLockCookie.Create())
            {
                using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                {
                    var de = DeclaredElementPointer.FindDeclaredElement();
                    if (de == null)
                        return "INVALID";
                    
                    if (IsRelatedToScriptableObject())
                    {
                        var value = InspectorVariableUsage.Value.GetFullPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement(), true);
                        return $"{de.ShortName} = {value}";
                    }
                    else
                    {
                        var value = InspectorVariableUsage.Value.GetPresentation(GetSolution(), DeclaredElementPointer.FindDeclaredElement(), true);
                        return $"{de.ShortName} = {value}";
                    }
                }
            }
        }

        public override IconId GetIcon()
        {
            if (IsPrefabModification)
                return UnityFileTypeThemedIcons.FileUnityPrefab.Id;

            return base.GetIcon();
        }
    }
}