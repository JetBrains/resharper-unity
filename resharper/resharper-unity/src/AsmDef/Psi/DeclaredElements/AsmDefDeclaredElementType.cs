using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements
{
    public class AsmDefDeclaredElementType : JsonNewDeclaredElementType
    {
        public static readonly AsmDefDeclaredElementType AsmDef = new("assembly definition",
            ProjectModelThemedIcons.Assembly.Id);

        private AsmDefDeclaredElementType(string name, [CanBeNull] IconId imageName)
            : base(name, imageName)
        {
        }

        // The name for an assembly definition must be a valid assembly name, which is the same as a dot separated set
        // of identifier names
        public override bool IsValidName(string name) => name.Split('.').All(NamingUtil.IsIdentifier);
    }
}
