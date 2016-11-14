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

        public UnityType(IClrTypeName typeName, IEnumerable<UnityEventFunction> eventFunctions)
        {
            myTypeName = typeName;
            EventFunctions = eventFunctions;
        }

        [NotNull]
        public IEnumerable<UnityEventFunction> EventFunctions { get; }

        [CanBeNull]
        public ITypeElement GetType([NotNull] IPsiModule module)
        {
            var type = TypeFactory.CreateTypeByCLRName(myTypeName, module);
            return type.GetTypeElement();
        }

        public bool HasEventFunction([NotNull] IMethod method)
        {
            return EventFunctions.Any(m => m.Match(method));
        }
    }
}