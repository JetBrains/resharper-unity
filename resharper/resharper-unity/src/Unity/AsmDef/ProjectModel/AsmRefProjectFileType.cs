using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class AsmRefProjectFileType : JsonNewProjectFileType
    {
        public new const string Name = "ASMREF";
        public const string ASMREF_EXTENSION = ".asmref";

        [CanBeNull, UsedImplicitly]
        public new static AsmRefProjectFileType Instance { get; private set; }

        public AsmRefProjectFileType()
            : base(Name, Strings.AsmRefProjectFileType_AsmRefProjectFileType_Assembly_Definition_Reference__Unity_, new[] { ASMREF_EXTENSION })
        {
        }
    }
}