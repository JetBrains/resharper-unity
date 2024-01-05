using JetBrains.Annotations;
using Mono.Debugger.Soft;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Marshallable;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    public class UnityTextureDebuggerHelper : UnityDebuggerHelper
    {
        private const string RequiredType = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter";
        public const string AssemblyName = "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture";

        private const string GetPixelsMethodName = "GetTexturePixelsInfo";
        private static readonly MethodSelector ourGetPixelsMethodFilter = new(m => m.Name == GetPixelsMethodName && m.Parameters.Length == 1);

        private UnityTextureDebuggerHelper(IReifiedType<Value> reifiedType, IDomainKnownTypes<Value> domainTypes) : base(reifiedType, domainTypes)
        {
        }
        
        [MustUseReturnValue]
        public ICallable<Value> GetPixels(Value value)
        {
            return Get(ourGetPixelsMethodFilter, ValueMarshallers.Value(value));
        }

        public static UnityTextureDebuggerHelper CreateHelper(IStackFrame frame, IValueFetchOptions options,
            IKnownTypes<Value> knownTypes, string assemblyLocation)
        {
            return CreateUnityDebuggerHelper<UnityTextureDebuggerHelper>(frame, options, knownTypes, assemblyLocation, AssemblyName,
                RequiredType, (reifiedType, domainTypes) => new UnityTextureDebuggerHelper(reifiedType, domainTypes));
        }
    }
}