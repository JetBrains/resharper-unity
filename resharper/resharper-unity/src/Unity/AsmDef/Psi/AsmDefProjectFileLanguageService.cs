using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi
{
    [ProjectFileType(typeof(AsmDefProjectFileType))]
    public class AsmDefProjectFileLanguageService : JsonNewProjectFileLanguageService
    {
        public override IconId Icon => UnityFileTypeThemedIcons.Asmdef.Id;
    }

    [ProjectFileType(typeof(AsmRefProjectFileType))]
    public class AsmRefProjectFileLanguageService : JsonNewProjectFileLanguageService
    {
        public override IconId Icon => UnityFileTypeThemedIcons.Asmref.Id;
    }
}