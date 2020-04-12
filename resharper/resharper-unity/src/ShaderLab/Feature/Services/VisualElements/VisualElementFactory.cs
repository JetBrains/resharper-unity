using System;
using System.Drawing;
using System.Globalization;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.VisualElements;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Colors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.VisualElements
{
    [Language(typeof(ShaderLabLanguage))]
    public class VisualElementFactory : IVisualElementFactory
    {
        public IColorReference GetColorReference(ITreeNode element)
        {
            if (element is IVectorLiteral vectorLiteral)
            {
                var vectorPropertyValue = VectorPropertyValueNavigator.GetByVector(vectorLiteral);
                if (vectorPropertyValue != null)
                {
                    // Does the vector literal belong to a vector property or a color property?
                    var propertyDeclation = PropertyDeclarationNavigator.GetByPropertValue(vectorPropertyValue);
                    if (propertyDeclation == null)
                        return null;

                    if (!(propertyDeclation.PropertyType is ISimplePropertyType simplePropertyType)
                        || simplePropertyType.Keyword?.NodeType != ShaderLabTokenType.COLOR_KEYWORD)
                    {
                        return null;
                    }
                }

                var values = vectorLiteral.Numbers;
                if (values.Count == 3 || values.Count == 4)
                {
                    var r = GetColorValue(values[0]);
                    var g = GetColorValue(values[1]);
                    var b = GetColorValue(values[2]);
                    // Technically, the colour should have 4 values, but show the highlight even if we're unfinished
                    var a = values.Count == 4 ? GetColorValue(values[3]) : 255;
                    var colorElement = new ColorElement(Color.FromArgb(a, r, g, b));
                    var range = GetDocumentRange(values[0], values.Last());
                    return new ShaderLabColorReference(colorElement,
                        VectorPropertyValueNavigator.GetByVector(vectorLiteral),
                        ColorValueNavigator.GetByConstant(vectorLiteral), range);
                }
            }
            return null;
        }

        private DocumentRange GetDocumentRange(INumericValue firstValue, INumericValue lastValue)
        {
            var startOffset = firstValue.GetDocumentStartOffset();
            var endOffset = lastValue.GetDocumentEndOffset();
            return new DocumentRange(startOffset, (DocumentOffset) endOffset);
        }

        private static int GetColorValue(INumericValue value)
        {
            double.TryParse(value.Constant.GetText(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d);
            return Math.Min(Math.Abs((int) (255 * d)), byte.MaxValue);
        }
    }
}