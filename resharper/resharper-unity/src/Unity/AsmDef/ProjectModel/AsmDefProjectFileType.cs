using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;

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
            : base(Name, Strings.AsmDefProjectFileType_AsmDefProjectFileType_Assembly_Definition__Unity_, new[] { ASMDEF_EXTENSION })
        {
        }
    }
}