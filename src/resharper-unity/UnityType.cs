using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public class UnityType
    {
        private readonly IClrTypeName typeName;

        public UnityType(IClrTypeName typeName, IEnumerable<UnityMessage> messages)
        {
            this.typeName = typeName;
            Messages = messages;
        }

        [NotNull]
        public IEnumerable<UnityMessage> Messages { get; }

        [CanBeNull]
        public ITypeElement GetType([NotNull] IPsiModule module)
        {
            var type = TypeFactory.CreateTypeByCLRName(typeName, module);
            return type.GetTypeElement();
        }

        public bool Contains([NotNull] IMethod method)
        {
            return Messages.Any(m => m.Match(method));
        }
    }
}