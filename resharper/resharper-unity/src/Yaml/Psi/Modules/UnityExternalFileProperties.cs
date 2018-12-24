using System.Collections.Generic;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    public class UnityExternalFileProperties : IPsiSourceFileProperties
    {
        private readonly IProperty<bool> myEnabled;

        public UnityExternalFileProperties(IProperty<bool> enabled)
        {
            myEnabled = enabled;
        }

        public IEnumerable<string> GetPreImportedNamespaces() => EmptyList<string>.InstanceList;
        public string GetDefaultNamespace() => string.Empty;
        public ICollection<PreProcessingDirective> GetDefines() => EmptyList<PreProcessingDirective>.InstanceList;
        public bool ShouldBuildPsi => myEnabled.Value;
        public bool IsGeneratedFile => false;
        public bool IsICacheParticipant => true;
        public bool ProvidesCodeModel => true;

        // TODO: Setting this to true disables daemon. Do we want this?
        public bool IsNonUserFile => false;
    }
}