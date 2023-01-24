using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.InputActions.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi
{
    [ProjectFileType(typeof(InputActionsProjectFileType))]
    public class InputActionsProjectFileLanguageService : JsonNewProjectFileLanguageService
    {
        public override IconId Icon => UnityFileTypeThemedIcons.UsageInputActions.Id;
    }
}