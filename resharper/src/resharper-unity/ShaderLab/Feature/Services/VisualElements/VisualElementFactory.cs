using System.Drawing;
using System.Globalization;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.VisualElements;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Colors;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
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
            if (element is IColorLiteral colorLiteral)
            {
                var values = colorLiteral.Numbers;
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
                        ColorPropertyValueNavigator.GetByColor(colorLiteral),
                        ColorValueNavigator.GetByConstant(colorLiteral), range);
                }
            }
            return null;
        }

        private DocumentRange GetDocumentRange(INumericValue firstValue, INumericValue lastValue)
        {
            var startOffset = firstValue.GetDocumentStartOffset();
            var endOffset = lastValue.GetDocumentEndOffset();
            return new DocumentRange(startOffset, endOffset);
        }

        private int GetColorValue(INumericValue value)
        {
            double.TryParse(value.Constant.GetText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
            return (int) (255 * d);
        }
    }
}