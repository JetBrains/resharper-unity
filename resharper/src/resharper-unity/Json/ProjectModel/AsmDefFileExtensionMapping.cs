using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.ProjectModel
{
    // Ideally, this file mapping should come automatically from the filenames
    // and masks in catalog.json. See RSRP-467093
    [ShellComponent]
    public class AsmDefFileExtensionMapping : IFileExtensionMapping
    {
        public AsmDefFileExtensionMapping(Lifetime lifetime)
        {
            Changed = new SimpleSignal(lifetime, "AsmDefFileExtensionMapping::Changed");
        }

        public IEnumerable<ProjectFileType> GetFileTypes(string extension)
        {
            if (extension.Equals(".asmdef", StringComparison.InvariantCultureIgnoreCase) && JsonProjectFileType.Instance != null)
                return new[] {JsonProjectFileType.Instance};
            return EmptyList<ProjectFileType>.Enumerable;
        }

        public IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
        {
            if (Equals(projectFileType, JsonProjectFileType.Instance))
                return new[] {".asmdef"};
            return EmptyList<string>.Instance;
        }

        public ISimpleSignal Changed { get; }
    }
}
