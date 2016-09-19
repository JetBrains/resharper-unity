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
        private readonly IClrTypeName myTypeName;

        public UnityType(IClrTypeName typeName, IEnumerable<UnityMessage> messages)
        {
            myTypeName = typeName;
            Messages = messages;
        }

        [NotNull]
        public IEnumerable<UnityMessage> Messages { get; }

        [CanBeNull]
        public ITypeElement GetType([NotNull] IPsiModule module)
        {
            var type = TypeFactory.CreateTypeByCLRName(myTypeName, module);
            return type.GetTypeElement();
        }

        public bool Contains([NotNull] IMethod method)
        {
            return Messages.Any(m => m.Match(method));
        }
    }
}