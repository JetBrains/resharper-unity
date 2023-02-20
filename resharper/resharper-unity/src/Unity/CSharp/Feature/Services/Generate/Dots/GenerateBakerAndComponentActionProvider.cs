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
    [GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateBakerAndComponent, typeof(CSharpLanguage))]
    public class GenerateBakerAndComponentActionProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.ClassDeclaration.IsFromUnityProject())
                return;

            if (context.ClassDeclaration.DeclaredElement is not IClass typeElement)
                return;

            // CompactOneToListMap is optimised for the typical use case of only one item per key
            var existingFieldsAndProperties = new Dictionary<string, ITypeOwner>();
            foreach (var typeMemberInstance in typeElement.GetAllClassMembers<IField>())
            {
                var field = typeMemberInstance.Member;
                if (!field.IsStatic && field.GetAccessRights() == AccessRights.PUBLIC)
                    existingFieldsAndProperties.Add(field.ShortName, field);
            }
            
            foreach (var typeMemberInstance in typeElement.GetAllClassMembers<IProperty>())
            {
                var property = typeMemberInstance.Member;
                if (!property.IsStatic && property.GetAccessRights() == AccessRights.PUBLIC)
                    existingFieldsAndProperties.Add(property.ShortName, property);
            }

            var elements = new List<GeneratorDeclaredElement>();

            foreach (var (_, typeOwner) in existingFieldsAndProperties)
            {
                Assertion.AssertNotNull(typeOwner);
                elements.Add(new GeneratorDeclaredElement(typeOwner, typeOwner.IdSubstitution));
            }

            context.ProvidedElements.AddRange(elements);
        }

        public override double Priority => 100;
    }
}