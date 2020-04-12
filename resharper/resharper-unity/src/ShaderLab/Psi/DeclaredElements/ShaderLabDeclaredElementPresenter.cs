using System.Text;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.DeclaredElements
{
    [PsiSharedComponent]
    public class ShaderLabDeclaredElementPresenter : IDeclaredElementPresenter
    {
        public static ShaderLabDeclaredElementPresenter Instance => PsiShared.GetComponent<ShaderLabDeclaredElementPresenter>();

        public RichText Format(DeclaredElementPresenterStyle style, IDeclaredElement element, ISubstitution substitution,
            out DeclaredElementPresenterMarking marking)
        {
            marking = new DeclaredElementPresenterMarking();
            if (!(element is IShaderLabDeclaredElement))
                return null;

            var result = new StringBuilder();

            if (style.ShowEntityKind != EntityKindForm.NONE)
            {
                var entityKind = GetEntityKind(element);
                if (entityKind != string.Empty)
                {
                    if (style.ShowEntityKind == EntityKindForm.NORMAL_IN_BRACKETS)
                        entityKind = "(" + entityKind + ")";
                    marking.EntityKindRange = AppendString(result, entityKind);
                    result.Append(" ");
                }
            }

            if (style.ShowNameInQuotes)
                result.Append("\'");

            if (style.ShowType == TypeStyle.DEFAULT)
            {
                var typeName = GetTypeName(element);
                if (!string.IsNullOrEmpty(typeName))
                {
                    marking.TypeRange = marking.ScalarTypeRange = AppendString(result, typeName);
                    result.Append(" ");
                }
            }

            if (style.ShowName != NameStyle.NONE)
            {
                marking.NameRange = AppendString(result, element.ShortName);
                result.Append(" ");
            }

            if (style.ShowType == TypeStyle.AFTER)
            {
                var typeName = GetTypeName(element);
                if (!string.IsNullOrEmpty(typeName))
                {
                    result.Append(": ");
                    marking.TypeRange = marking.ScalarTypeRange = AppendString(result, typeName);
                    result.Append(" ");
                }
            }

            if (style.ShowNameInQuotes)
            {
                TrimString(result);
                result.Append("\'");
            }

            if (style.ShowName != NameStyle.NONE && style.ShowName != NameStyle.SHORT &&
                style.ShowName != NameStyle.SHORT_RAW)
            {
                AppendDisplayName(result, element);
            }

            if (style.ShowConstantValue)
            {
            }

            TrimString(result);
            return result.ToString();
        }

        public string Format(ParameterKind parameterKind) => string.Empty;
        public string Format(AccessRights accessRights) => string.Empty;

        private static void TrimString(StringBuilder sb)
        {
            while (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Remove(sb.Length - 1, 1);
        }

        private static TextRange AppendString(StringBuilder sb, string substr)
        {
            var s = sb.Length;
            sb.Append(substr);
            return substr.Length == 0 ? TextRange.InvalidRange : new TextRange(s, sb.Length);
        }

        private static string GetEntityKind(IDeclaredElement element)
        {
            return element is IPropertyDeclaredElement ? "property" : string.Empty;
        }

        private string GetTypeName(IDeclaredElement element)
        {
            if (element is IPropertyDeclaredElement property)
                return property.GetPropertyType();
            return string.Empty;
        }

        private void AppendDisplayName(StringBuilder result, IDeclaredElement element)
        {
            if (element is IPropertyDeclaredElement property)
            {
                var displayName = property.GetDisplayName();
                if (!string.IsNullOrEmpty(displayName))
                {
                    result.Append(displayName);
                    result.Append(" ");
                }
            }
        }
    }
}