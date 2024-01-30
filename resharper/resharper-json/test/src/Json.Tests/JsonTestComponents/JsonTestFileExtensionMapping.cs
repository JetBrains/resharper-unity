using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.JsonTestComponents
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class JsonTestFileExtensionMapping : FileTypeDefinitionExtensionMapping
    {
        private const string JSON_EXTENSION = ".json";

        public JsonTestFileExtensionMapping(Lifetime lifetime, IProjectFileTypes fileTypes)
            : base(lifetime, fileTypes)
        {
        }

        public override IEnumerable<ProjectFileType> GetFileTypes(string extension)
        {
            return extension.Equals(JSON_EXTENSION, StringComparison.InvariantCultureIgnoreCase)
                ? new[] { JsonNewProjectFileType.Instance }
                : base.GetFileTypes(extension);
        }

        public override IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
        {
            return Equals(projectFileType, JsonNewProjectFileType.Instance)
                ? base.GetExtensions(projectFileType).Concat(JSON_EXTENSION)
                : base.GetExtensions(projectFileType);
        }
    }
}