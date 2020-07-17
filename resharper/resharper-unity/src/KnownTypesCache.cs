using System.Collections.Concurrent;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class KnownTypesCache
    {
        private readonly ConcurrentDictionary<IClrTypeName, IDeclaredType> myTypes = new ConcurrentDictionary<IClrTypeName, IDeclaredType>();

        [NotNull]
        public IDeclaredType GetByClrTypeName(IClrTypeName typeName, IPsiModule module)
        {
            return module.GetPredefinedType().TryGetType(typeName, NullableAnnotation.Unknown)
                ?? myTypes.GetOrAdd(typeName, name => TypeFactory.CreateTypeByCLRName(name, module));
        }
    }
}