using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Colors
{
    public class ShaderLabColorReference : IColorReference
    {
        private readonly IColorPropertyValue myColorPropertyValue;
        private readonly IColorValue myColorValue;

        public ShaderLabColorReference(IColorElement colorElement, IColorPropertyValue colorPropertyValue, IColorValue colorValue, DocumentRange colorConstantRange)
        {
            myColorPropertyValue = colorPropertyValue;
            myColorValue = colorValue;
            ColorElement = colorElement;
            Owner = (ITreeNode) colorValue ?? colorPropertyValue;
            ColorConstantRange = colorConstantRange;

            BindOptions = new ColorBindOptions
            {
                BindsToName = false,
                BindsToValue = true
            };
        }

        public void Bind(IColorElement colorElement)
        {
            var languageService = ShaderLabLanguage.Instance.LanguageService();
            Assertion.AssertNotNull(languageService, "languageService != null");
            var lexer = languageService.GetPrimaryLexerFactory().CreateLexer(new StringBuffer(
                $"({colorElement.RGBColor.R/255.0:0.##}, {colorElement.RGBColor.G/255.0:0.##}, {colorElement.RGBColor.B/255.0:0.##}, {colorElement.RGBColor.A/255.0:0.##})"));
            var parser = (IShaderLabParser) languageService.CreateParser(lexer, null, null);
            var newLiteral = parser.ParseColorLiteral();

            myColorPropertyValue?.SetColor(newLiteral);
            myColorValue?.SetConstant(newLiteral);
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return EmptyList<IColorElement>.Instance;
        }

        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }
        public ColorBindOptions BindOptions { get; }
    }
}