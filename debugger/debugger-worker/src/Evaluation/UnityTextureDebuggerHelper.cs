using JetBrains.Annotations;
using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Marshallable;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    public class UnityTextureDebuggerHelper<TValue> : UnityDebuggerHelper<TValue> where TValue : class
    {
        private const string RequiredType = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter";
        private const string AssemblyBaseName = "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture";

        private const string GetPixelsMethodName = "GetTexturePixelsInfo";
        private static readonly MethodSelector ourGetPixelsMethodFilter = new(m => m.Name == GetPixelsMethodName && m.Parameters.Length == 1);

        private UnityTextureDebuggerHelper(IReifiedType<TValue> reifiedType, IDomainKnownTypes<TValue> domainTypes) : base(reifiedType, domainTypes)
        {
        }
        
        [MustUseReturnValue]
        public ICallable<TValue> GetPixels(TValue value)
        {
            return Get(ourGetPixelsMethodFilter, ValueMarshallers.Value(value));
        }

        public static UnityTextureDebuggerHelper<TValue> CreateHelper(IStackFrame frame, IValueFetchOptions options,
            IKnownTypes<TValue> knownTypes, UnityBundleInfo assemblyBundleInfo)
        {
            return CreateUnityDebuggerHelper<UnityTextureDebuggerHelper<TValue>>(frame, options, knownTypes, assemblyBundleInfo,
                RequiredType, (reifiedType, domainTypes) => new UnityTextureDebuggerHelper<TValue>(reifiedType, domainTypes));
        }

        public static string GetAssemblyName(bool isDotNetCore) => GetAssemblyName(AssemblyBaseName, isDotNetCore);
    }
}