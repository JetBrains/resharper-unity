using JetBrains.Annotations;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.DeclaredElements;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements
{
    public class AsmDefDeclaredElementType : JsonNewDeclaredElementType
    {
        private AsmDefDeclaredElementType(string name, [CanBeNull] IconId imageName)
            : base(name, imageName)
        {
        }

        public static readonly AsmDefDeclaredElementType AsmDef =
            new AsmDefDeclaredElementType("assembly definition", ProjectModelThemedIcons.Assembly.Id);
    }
}
