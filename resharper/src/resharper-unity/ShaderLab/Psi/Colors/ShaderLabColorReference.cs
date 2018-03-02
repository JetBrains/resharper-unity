using System.Collections.Generic;
using System.Globalization;
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
        private readonly IVectorPropertyValue myVectorPropertyValue;
        private readonly IColorValue myColorValue;

        public ShaderLabColorReference(IColorElement colorElement, IVectorPropertyValue vectorpertyValue,
            IColorValue colorValue, DocumentRange colorConstantRange)
        {
            myVectorPropertyValue = vectorpertyValue;
            myColorValue = colorValue;
            ColorElement = colorElement;
            Owner = (ITreeNode) colorValue ?? vectorpertyValue;
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
            var formattedString = GetFormattedString(colorElement);
            var lexer = languageService.GetPrimaryLexerFactory().CreateLexer(new StringBuffer(formattedString));
            var parser = (IShaderLabParser)languageService.CreateParser(lexer, null, null);
            var newLiteral = parser.ParseVectorLiteral();

            // One of these will be null
            myVectorPropertyValue?.SetVector(newLiteral);
            myColorValue?.SetConstant(newLiteral);
        }

        private static string GetFormattedString(IColorElement colorElement)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "({0:0.##}, {1:0.##}, {2:0.##}, {3:0.##})",
                colorElement.RGBColor.R / 255.0,
                colorElement.RGBColor.G / 255.0,
                colorElement.RGBColor.B / 255.0,
                colorElement.RGBColor.A / 255.0);
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