using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class InputActionsProjectFileType : JsonNewProjectFileType
    {
        public new const string Name = "INPUTACTIONS";
        public const string INPUTACTIONS_EXTENSION = ".inputactions";

        [CanBeNull, UsedImplicitly]
        public new static InputActionsProjectFileType Instance { get; private set; }

        public InputActionsProjectFileType()
            : base(Name, Strings.InputActionsProjectFileType_InputActionsProjectFileType_InputActions__Unity_, new[] { INPUTACTIONS_EXTENSION })
        {
        }
    }
}