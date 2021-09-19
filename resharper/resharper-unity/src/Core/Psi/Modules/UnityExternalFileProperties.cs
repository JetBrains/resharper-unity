using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    public class UnityExternalFileProperties : IPsiSourceFileProperties
    {
        public UnityExternalFileProperties(bool isGeneratedFile, bool isNonUserFile)
        {
            IsGeneratedFile = isGeneratedFile;
            IsNonUserFile = isNonUserFile;
        }

        public IEnumerable<string> GetPreImportedNamespaces() => EmptyList<string>.InstanceList;
        public string GetDefaultNamespace() => string.Empty;
        public ICollection<PreProcessingDirective> GetDefines() => EmptyList<PreProcessingDirective>.InstanceList;
        public bool ShouldBuildPsi => true;
        public bool IsGeneratedFile { get; }
        public bool IsICacheParticipant => true;
        public bool ProvidesCodeModel => true;
        public bool IsNonUserFile { get; }
    }
}
