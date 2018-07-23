using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    public static class AttributesUtil
    {
        public static SmartList<IField> GetFieldsByAttribute(this IAttribute attribute)
        {
            var list = new SmartList<IField>();
            foreach (var fieldDeclaration in FieldDeclarationNavigator.GetByAttribute(attribute))
            {
                if (fieldDeclaration.DeclaredElement != null)
                    list.Add(fieldDeclaration.DeclaredElement);
            }
            foreach (var constantDeclaration in ConstantDeclarationNavigator.GetByAttribute(attribute))
            {
                if (constantDeclaration.DeclaredElement != null)
                    list.Add(constantDeclaration.DeclaredElement);
            }

            return list;
        }
    }
}