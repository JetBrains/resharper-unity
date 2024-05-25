#nullable enable

using System;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.QuickDoc
{
    [QuickDocProvider(0, Instantiation.DemandAnyThreadUnsafe)]
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

        private IShaderLabCommandDeclaredElement? GetShaderLabCommandDeclaredElement(IDataContext context)
        {
            var nodes = context.GetData(PsiDataConstants.SELECTED_TREE_NODES);
            return nodes?.FirstOrDefault(it => it.GetTokenType() is IShaderLabTokenNodeType tokenType && tokenType.GetKeywordType(it).IsCommandKeyword())?.GetContainingNode<ShaderLabCommandBase>()?.DeclaredElement as IShaderLabCommandDeclaredElement;
        }
    }
}
