using System.Text;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.DeclaredElements
{
    [PsiSharedComponent]
    public class JsonNewDeclaredElementPresenter : IDeclaredElementPresenter
    {
        public static JsonNewDeclaredElementPresenter Instance => PsiShared.GetComponent<JsonNewDeclaredElementPresenter>();

        public RichText Format(DeclaredElementPresenterStyle style, IDeclaredElement element, ISubstitution substitution,
            out DeclaredElementPresenterMarking marking)
        {
            marking = new DeclaredElementPresenterMarking();
            if (!(element is JsonNewDeclaredElementBase))
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

            if (style.ShowName != NameStyle.NONE)
            {
                marking.NameRange = AppendString(result, element.ShortName);
                result.Append(" ");
            }

            if (style.ShowNameInQuotes)
            {
                TrimString(result);
                result.Append("\'");
            }

            if (style.ShowConstantValue)
            {
            }

            TrimString(result);
            return result.ToString();
        }

        public string Format(ParameterKind parameterKind) => string.Empty;
        public string Format(AccessRights accessRights) => string.Empty;

        public string GetEntityKind(IDeclaredElement declaredElement)
        {
            var elementType = declaredElement.GetElementType() as JsonNewDeclaredElementType;
            if (elementType != null)
                return elementType.PresentableName.ToLower();

            return string.Empty;
        }
        
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
    }
}