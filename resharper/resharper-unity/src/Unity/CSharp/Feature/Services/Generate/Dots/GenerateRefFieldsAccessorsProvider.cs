#nullable enable

using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    [GeneratorElementProvider(GeneratorUnityKinds.UnityGenerateRefAccessors, typeof(CSharpLanguage))]
    public class GenerateRefAccessorsProvider : GeneratorProviderBase<CSharpGeneratorContext>
    {
        public override double Priority => 101;

        public override void Populate(CSharpGeneratorContext context)
        {
            if (!context.ClassDeclaration.IsFromUnityProject())
                return;
            var node = context.Anchor;
            var (sourceType, _) = UnityApiExtensions.GetReferencedType(node.GetContainingNode<IFieldDeclaration>());

            if (sourceType == null)
                return;

            foreach (var field in sourceType.Fields)
            {
                if (field.GetAccessRights() == AccessRights.PUBLIC)
                    context.ProvidedElements.Add(new GeneratorDeclaredElement(field, field.IdSubstitution));
            }

            foreach (var property in sourceType.Properties)
            {
                if (property.GetAccessRights() == AccessRights.PUBLIC)
                    context.ProvidedElements.Add(new GeneratorDeclaredElement(property, property.IdSubstitution));
            }
        }
    }
}