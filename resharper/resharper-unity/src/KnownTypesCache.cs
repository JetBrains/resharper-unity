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
            var type = module.GetPredefinedType().TryGetType(typeName, NullableAnnotation.Unknown);
            if (type != null)
                return type;

            // Make sure the type is still valid before handing it out. It might be invalid if the module used to create
            // it has been changed
            type = myTypes.AddOrUpdate(typeName, name => TypeFactory.CreateTypeByCLRName(name, module),
                (name, existingValue) => existingValue.Module.IsValid()
                    ? existingValue
                    : TypeFactory.CreateTypeByCLRName(name, module));
            return type;
        }

        private IDeclaredType ValueFactory(IClrTypeName arg)
        {
            throw new System.NotImplementedException();
        }
    }
}
