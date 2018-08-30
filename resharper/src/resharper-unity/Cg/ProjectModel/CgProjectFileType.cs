using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class CgProjectFileType : KnownProjectFileType
    {
        public new const string Name = "CG";
        
        // Don't forget to update list in CgFileTypeFactory on the frontend 
        public const string CG_EXTENSION = ".cginc";
        public const string COMPUTE_EXTENSION = ".compute";
        public const string HLSL_EXTENSION = ".hlsl";
        public const string GLSL_EXTENSION = ".glsl";
        public const string HLSLINC_EXTENSION = ".hlslinc";
        public const string GLSLINC_EXTENSION = ".glslinc";

        public new static readonly CgProjectFileType Instance = null;

        public CgProjectFileType()
            : base(Name, "Cg", new[] {CG_EXTENSION, COMPUTE_EXTENSION, HLSL_EXTENSION, GLSL_EXTENSION, HLSLINC_EXTENSION, GLSLINC_EXTENSION})
        {
        }
    }
}