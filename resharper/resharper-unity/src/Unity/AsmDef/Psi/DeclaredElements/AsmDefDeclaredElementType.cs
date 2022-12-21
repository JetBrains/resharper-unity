using System.Linq;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.UI.Icons;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements
{
    public class AsmDefDeclaredElementType : JsonNewDeclaredElementType
    {
        public static readonly AsmDefDeclaredElementType AsmDef = new("assembly definition",
            UnityFileTypeThemedIcons.Asmdef.Id);

        private AsmDefDeclaredElementType(string name, IconId? imageName)
            : base(name, imageName)
        {
        }

        // The name for an assembly definition must be a valid assembly name, which is the same as a dot separated set
        // of identifier names
        public override bool IsValidName(string name) => name.Split('.').All(NamingUtil.IsIdentifier);
    }
}
