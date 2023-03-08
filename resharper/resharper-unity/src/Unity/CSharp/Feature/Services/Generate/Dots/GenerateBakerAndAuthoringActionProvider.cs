using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateBakerAndAuthoring, typeof(CSharpLanguage))]
    public class GenerateBakerAndAuthoringActionProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.ClassDeclaration.IsFromUnityProject())
                return;

            var classDeclarationDeclaredElement = context.ClassDeclaration.DeclaredElement;
            if (classDeclarationDeclaredElement == null)
                return;

            var existingFields = new Dictionary<string, IField>();
            foreach (var typeMemberInstance in classDeclarationDeclaredElement.GetAllClassMembers<IField>())
            {
                var field = typeMemberInstance.Member;
                if (!field.IsStatic && field.GetAccessRights() == AccessRights.PUBLIC)
                    existingFields.Add(field.ShortName, field);
            }

            var elements = new List<GeneratorDeclaredElement>();

            foreach (var (_, field) in existingFields)
            {
                Assertion.AssertNotNull(field);
                elements.Add(new GeneratorDeclaredElement(field, field.IdSubstitution));
            }

            context.ProvidedElements.AddRange(elements);
        }

        public override double Priority => 100;
    }
}