using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ShellComponent]
    public class JsonNewTestFileExtensionMapping : FileTypeDefinitionExtensionMapping
    {
        public JsonNewTestFileExtensionMapping(Lifetime lifetime, IProjectFileTypes fileTypes)
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