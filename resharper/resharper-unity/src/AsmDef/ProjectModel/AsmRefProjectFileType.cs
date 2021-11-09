using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.ProjectModel;

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
            : base(Name, "Assembly Definition Reference (Unity)", new[] { ASMREF_EXTENSION })
        {
        }
    }
}