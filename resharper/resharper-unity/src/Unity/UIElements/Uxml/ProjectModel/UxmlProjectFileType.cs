using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class UxmlProjectFileType : XmlProjectFileType
    {
        public new const string Name = "UXML";
        public const string UXML_EXTENSION = ".uxml";

        [CanBeNull, UsedImplicitly]
        public new static UxmlProjectFileType Instance { get; private set; }

        public UxmlProjectFileType()
            : base(Name, Strings.UxmlUnity_Text, new[] { UXML_EXTENSION })
        {
        }
    }
}