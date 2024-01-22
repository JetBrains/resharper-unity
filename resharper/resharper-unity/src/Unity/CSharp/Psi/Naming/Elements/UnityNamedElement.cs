using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Naming.Elements;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements
{
    // We could pass null here, but we only actually support C#...
    [NamedElementsBag(typeof(CSharpLanguage))]
    public class UnityNamedElement : ElementKindOfElementType
    {
        [UsedImplicitly] public static readonly IElementKind SERIALISED_FIELD =
            new UnityNamedElement("UNITY_SERIALISED_FIELD", typeof(Strings), nameof(Strings.UnitySerializedField_PresentableName_Text), IsSerialisedField, new NamingRule
            {
                NamingStyleKind = NamingStyleKinds.aaBb
            });

        private readonly NamingRule myNamingRule;

        [Obsolete("Consider to use overload with resourceType and resourceName instead of presentableName.")]
        protected UnityNamedElement(string name, string presentableName, Func<IDeclaredElement, bool> isApplicable,
                        NamingRule namingRule)
            : base(name, presentableName, isApplicable, modifier:RoslynNamingSymbolModifier.UntranslatableToRoslyn)
        {
            myNamingRule = namingRule;
        }
        
        protected UnityNamedElement(string name, Type resourceType, string resourceName, Func<IDeclaredElement, bool> isApplicable, NamingRule namingRule)
            : base(name, resourceType, resourceName, isApplicable, modifier:RoslynNamingSymbolModifier.UntranslatableToRoslyn)
        {
            myNamingRule = namingRule;
        }

        public override PsiLanguageType Language => CSharpLanguage.Instance;

        // This doesn't really do anything useful. See UnityNamingRuleDefaultSettings
        public override NamingRule GetDefaultRule() => myNamingRule;

        private static bool IsSerialisedField(IDeclaredElement declaredElement)
        {
            if (!(declaredElement is IField field))
                return false;

            if (!declaredElement.IsFromUnityProject())
                return false;

            var solution = declaredElement.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();
            return unityApi.IsSerialisedField(field).Has(SerializedFieldStatus.UnitySerializedField);
        }
    }
}
