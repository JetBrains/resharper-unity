using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Json.ProjectModel
{
    // It would be nice to just map .asmdef to the JsonProjectFileType via IFileExtensionMapping, but Rider suppresses
    // solution wide analysis on JSON files, by looking to see if a file is of type JsonProjectFileType. If we create
    // our own file type that derives from JsonProjectFileType, we get all the benefits of JSON, and also appear in SWEA
    [ProjectFileTypeDefinition(Name)]
    public class AsmDefProjectFileType : JsonProjectFileType
    {
        public new const string Name = "ASMDEF";
        public const string ASMDEF_EXTENSION = ".asmdef";

        public new static readonly AsmDefProjectFileType Instance = null;

        public AsmDefProjectFileType()
            : base(Name, "Assembly Definition (Unity)", new[] { ASMDEF_EXTENSION })
        {
        }
    }
}