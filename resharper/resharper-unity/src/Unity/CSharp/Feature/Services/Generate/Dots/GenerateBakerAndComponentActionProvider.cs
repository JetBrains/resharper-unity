using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
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
        private static readonly HashSet<IClrTypeName> ourBottomLevelUnityTypes = new()
        {
            KnownTypes.MonoBehaviour,
            KnownTypes.Component,
            KnownTypes.ScriptableObject
        };
        
        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.ClassDeclaration.IsFromUnityProject())
                return;

            if (context.ClassDeclaration.DeclaredElement is not IClass typeElement)
                return;

            var existingFieldsAndProperties = new Dictionary<string, ITypeOwner>();

            var currentTypeElement = typeElement;

            while (currentTypeElement != null && currentTypeElement.DerivesFrom(KnownTypes.Component))
            {
                if(ourBottomLevelUnityTypes.Contains(currentTypeElement.GetClrName()))
                    break;
                
                CollectFieldsAndProperties(currentTypeElement, existingFieldsAndProperties);
                currentTypeElement = currentTypeElement.GetSuperClass();
            }
            

            var elements = new List<GeneratorDeclaredElement>();

            foreach (var (_, typeOwner) in existingFieldsAndProperties)
            {
                Assertion.AssertNotNull(typeOwner);
                elements.Add(new GeneratorDeclaredElement(typeOwner, typeOwner.IdSubstitution));
            }

            context.ProvidedElements.AddRange(elements);
        }

        private static void CollectFieldsAndProperties(IClass typeElement, Dictionary<string, ITypeOwner> existingFieldsAndProperties)
        {
            foreach (var field in typeElement.Fields)
            {
                // var field = typeMemberInstance;
                if (!field.IsStatic && field.GetAccessRights() == AccessRights.PUBLIC)
                    existingFieldsAndProperties.Add(field.ShortName, field);
            }

            foreach (var property in typeElement.Properties)
            {
                if (!property.IsStatic && property.GetAccessRights() == AccessRights.PUBLIC)
                    existingFieldsAndProperties.Add(property.ShortName, property);
            }
        }

        public override double Priority => 100;
    }
}