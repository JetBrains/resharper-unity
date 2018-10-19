using JetBrains.Annotations;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Services;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements
{
    public class AsmDefDeclaredElementType : JavaScriptDeclaredElementType
    {
        private AsmDefDeclaredElementType(string name, [CanBeNull] IconId imageName)
            : base(name, imageName)
        {
        }

        public static readonly AsmDefDeclaredElementType AsmDef =
            new AsmDefDeclaredElementType("assembly definition", ProjectModelThemedIcons.Assembly.Id);
    }
}
