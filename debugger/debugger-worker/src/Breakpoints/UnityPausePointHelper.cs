using JetBrains.Annotations;
using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Marshallable;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Breakpoints
{
    public class UnityPausePointHelper<TValue> : UnityDebuggerHelper<TValue> where TValue : class
    {
        private UnityPausePointHelper(IReifiedType<TValue> reifiedType, IDomainKnownTypes<TValue> domainTypes) : base(
            reifiedType, domainTypes)
        {
        }

        private const string RequiredType = "JetBrains.Debugger.Worker.Plugins.Unity.PausePoint.EndFrameSystem";
        private const string AssemblyBaseName = "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper";

        private const string MakePauseMethodName = "MakePause";
        private static readonly MethodSelector ourMakePauseMethodFilter =
            new(m => m.Name == MakePauseMethodName && m.Parameters.Length == 0);

        [MustUseReturnValue]
        public ICallable<TValue> RequestPause()
        {
            return Get(ourMakePauseMethodFilter);
        }

        public static UnityPausePointHelper<TValue> CreateHelper(IStackFrame frame, IValueFetchOptions options,
            IKnownTypes<TValue> knownTypes, UnityBundleInfo assemblyBundleInfo)
        {
           return CreateUnityDebuggerHelper<UnityPausePointHelper<TValue>>(frame, options, knownTypes, assemblyBundleInfo,
                RequiredType, (reifiedType, domainTypes) => new UnityPausePointHelper<TValue>(reifiedType, domainTypes));
        }

        public static string GetAssemblyName(bool isDotNetCore) => GetAssemblyName(AssemblyBaseName, isDotNetCore);
    }
}