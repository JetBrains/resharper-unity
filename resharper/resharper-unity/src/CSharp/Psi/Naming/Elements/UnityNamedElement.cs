using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Naming.Elements;
using JetBrains.ReSharper.Psi.Naming.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements
{
    // We could pass null here, but we only actually support C#...
    [NamedElementsBag(typeof(CSharpLanguage))]
    public class UnityNamedElement : ElementKindOfElementType
    {
        [UsedImplicitly] public static readonly IElementKind SERIALISED_FIELD =
            new UnityNamedElement("UNITY_SERIALISED_FIELD", "Unity serialized field", IsSerialisedField, new NamingRule
            {
                NamingStyleKind = NamingStyleKinds.aaBb
            });

        private readonly NamingRule myNamingRule;

        protected UnityNamedElement(string name, string presentableName, Func<IDeclaredElement, bool> isApplicable,
                        NamingRule namingRule)
            : base(name, presentableName, isApplicable)
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
            return unityApi.IsSerialisedField(field);
        }
    }
}
