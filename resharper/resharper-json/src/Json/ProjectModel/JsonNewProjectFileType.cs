using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Json.ProjectModel
{
    // Create a project file type we can register a PSI language against. We can't use the JsonProjectFileType in the
    // platform because we'll clash with ReSharper's JavaScript based PSI, which isn't available in Rider.
    [ProjectFileTypeDefinition(Name)]
    public class JsonNewProjectFileType : KnownProjectFileType
    {
        private new const string Name = "JSON_NEW";

        [CanBeNull, UsedImplicitly]
        public new static JsonNewProjectFileType Instance { get; private set; }

        public JsonNewProjectFileType()
            : base(Name, "JSON (standalone)")
        {
        }

        protected JsonNewProjectFileType(string name, string presentableName, IEnumerable<string> strings)
            : base(name, presentableName, strings)
        {
        }
    }
}