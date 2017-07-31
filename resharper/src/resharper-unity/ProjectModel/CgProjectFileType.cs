using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class CgProjectFileType : KnownProjectFileType
    {
        public new const string Name = "CG";
        public const string CG_EXTENSION = ".cginc";

        public new static readonly CgProjectFileType Instance = null;

        public CgProjectFileType()
            : base(Name, "Cg", new[] {CG_EXTENSION})
        {
        }
    }
}