#nullable enable

using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements
{
    public class InputActionsDeclaredElementType : JsonNewDeclaredElementType
    {
        public static readonly InputActionsDeclaredElementType InputActions = new("input actions",
            ProjectModelThemedIcons.Manifest.Id);

        private InputActionsDeclaredElementType(string name, IconId? imageName)
            : base(name, imageName)
        {
        }
    }
}
