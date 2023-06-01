#nullable enable

using System;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.QuickDoc
{
    [QuickDocProvider(0)]
    public class ShaderLabQuickDocProvider : IQuickDocProvider
    {
        private readonly ShaderLabDeclaredElementDescriptionProvider myDescriptionProvider;
        private readonly XmlDocHtmlPresenter myXmlDocHtmlPresenter; 

        public ShaderLabQuickDocProvider(ShaderLabDeclaredElementDescriptionProvider descriptionProvider, XmlDocHtmlPresenter xmlDocHtmlPresenter)
        {
            myDescriptionProvider = descriptionProvider;
            myXmlDocHtmlPresenter = xmlDocHtmlPresenter;
        }

        public bool CanNavigate(IDataContext context) => GetShaderLabCommandDeclaredElement(context) is not null;

        public void Resolve(IDataContext context, Action<IQuickDocPresenter, PsiLanguageType> resolved)
        {
            if (GetShaderLabCommandDeclaredElement(context) is not {} element)
                return;
            var description = myDescriptionProvider.GetElementDescription(element, DeclaredElementDescriptionStyle.FULL_STYLE, element.PresentationLanguage);
            if (!RichTextBlock.IsNullOrEmpty(description))
                resolved(new ShaderLabCommandQuickDocPresenter(element, description.Text, myXmlDocHtmlPresenter), ShaderLabLanguage.Instance);
        }

        private IDeclaredElement? GetShaderLabCommandDeclaredElement(IDataContext context)
        {
            var declaredElements = context.GetData(PsiDataConstants.DECLARED_ELEMENTS);
            return declaredElements?.FirstOrDefault(it => it.GetElementType() == ShaderLabDeclaredElementType.Command);
        }
    }
}