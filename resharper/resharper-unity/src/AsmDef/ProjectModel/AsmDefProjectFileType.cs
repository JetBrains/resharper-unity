using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class AsmDefProjectFileType : JsonNewProjectFileType
    {
        public new const string Name = "ASMDEF";
        public const string ASMDEF_EXTENSION = ".asmdef";

        [CanBeNull, UsedImplicitly]
        public new static AsmDefProjectFileType Instance { get; private set; }

        public AsmDefProjectFileType()
            : base(Name, "Assembly Definition (Unity)", new[] { ASMDEF_EXTENSION })
        {
        }
    }
}