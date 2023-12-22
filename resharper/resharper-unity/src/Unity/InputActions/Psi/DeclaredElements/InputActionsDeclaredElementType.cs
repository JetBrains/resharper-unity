#nullable enable

using JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements
{
    public class InputActionsDeclaredElementType : JsonNewDeclaredElementType
    {
        public static readonly InputActionsDeclaredElementType InputActions = new("input actions", 
            UnityFileTypeThemedIcons.InputActions.Id);

        private InputActionsDeclaredElementType(string name, IconId? imageName)
            : base(name, imageName)
        {
        }
    }
}
