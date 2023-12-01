using JetBrains.Annotations;
using Mono.Debugger.Soft;
using Mono.Debugging.Marshallable;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    public class UnityDebuggingHelper : KnownTypeBase<Value>
    {
        public const string RequiredType = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter";
        public const string AssemblyName = "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture";
        public static string RequiredTypeWithAssembly = $"{RequiredType}, {AssemblyName}";

        private const string GetPixelsMethodName = "GetPixelsInString";

        private static readonly MethodSelector ourGetPixelsMethodFilter =
            new(m => m.Name == GetPixelsMethodName && m.Parameters.Length == 1);

        
        public UnityDebuggingHelper(IReifiedType<Value> reifiedType, IDomainKnownTypes<Value> domainTypes) : base(reifiedType, domainTypes)
        {
        }
        
        [MustUseReturnValue]
        public ICallable<Value> GetPixels(Value value)
        {
            return Get(ourGetPixelsMethodFilter, ValueMarshallers.Value(value));
        }
    }
}