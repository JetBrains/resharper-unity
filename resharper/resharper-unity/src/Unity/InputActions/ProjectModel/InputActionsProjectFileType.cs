using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;

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
            : base(Name, "InputActions (Unity)", new[] { INPUTACTIONS_EXTENSION })
        {
        }
    }
}