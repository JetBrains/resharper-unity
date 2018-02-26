using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.ContextActions
{
    public static class AttributeUtil
    {
        public static void AddAttribute([CanBeNull] IFieldDeclaration fieldDeclaration, IClrTypeName attributeTypeName,
            IPsiModule psiModule, CSharpElementFactory elementFactory)
        {
            if (fieldDeclaration == null) return;

            var typeElement = TypeFactory.CreateTypeByCLRName(attributeTypeName, psiModule).GetTypeElement();
            if (typeElement != null)
            {
                var attribute = elementFactory.CreateAttribute(typeElement);
                fieldDeclaration.AddAttributeAfter(attribute, null);
            }
        }
    }
}