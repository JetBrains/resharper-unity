using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.JsonTestComponents
{
    [ShellComponent]
    public class JsonTestFileExtensionMapping : FileTypeDefinitionExtensionMapping
    {
        public JsonTestFileExtensionMapping(Lifetime lifetime, IProjectFileTypes fileTypes)
            : base(lifetime, fileTypes)
        {
        }

        public override IEnumerable<ProjectFileType> GetFileTypes(string extension)
        {
            if (extension.Equals(JsonProjectFileType.JSON_EXTENSION, StringComparison.InvariantCultureIgnoreCase))
                return new[] { JsonNewProjectFileType.Instance };
            return base.GetFileTypes(extension);
        }

        public override IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
        {
            if (Equals(projectFileType, JsonNewProjectFileType.Instance))
                return base.GetExtensions(projectFileType).Concat(JsonProjectFileType.JSON_EXTENSION);
            return base.GetExtensions(projectFileType);
        }
    }
}