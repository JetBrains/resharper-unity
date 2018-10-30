using System.Collections.Generic;
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
        public bool IsGeneratedFile => false;
        public bool IsICacheParticipant => true;
        public bool ProvidesCodeModel => true;

        // TODO: Setting this to true disables daemon. Do we want this?
        public bool IsNonUserFile => false;
    }
}