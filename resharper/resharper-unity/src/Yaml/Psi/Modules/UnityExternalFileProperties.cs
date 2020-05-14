using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    public class UnityExternalFileProperties : IPsiSourceFileProperties
    {
        public IEnumerable<string> GetPreImportedNamespaces() => EmptyList<string>.InstanceList;
        public string GetDefaultNamespace() => string.Empty;
        public ICollection<PreProcessingDirective> GetDefines() => EmptyList<PreProcessingDirective>.InstanceList;

        public bool ShouldBuildPsi => true;

        // ClrToDoManager takes a lot of time inside yaml files, but if file is generated, it will be ignored by todomanager
        public bool IsGeneratedFile => true;
        public bool IsICacheParticipant => true;
        public bool ProvidesCodeModel => true;
        public bool IsNonUserFile => false;
    }
}
