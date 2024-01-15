using JetBrains.Annotations;
using Mono.Debugger.Soft;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Marshallable;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Breakpoints
{
    public class UnityPausePointHelper : UnityDebuggerHelper
    {
        private UnityPausePointHelper(IReifiedType<Value> reifiedType, IDomainKnownTypes<Value> domainTypes) : base(
            reifiedType, domainTypes)
        {
        }

        private const string RequiredType = "JetBrains.Debugger.Worker.Plugins.Unity.PausePoint.EndFrameSystem";
        public const string AssemblyName = "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper";

        private const string MakePauseMethodName = "MakePause";
        private static readonly MethodSelector ourMakePauseMethodFilter =
            new(m => m.Name == MakePauseMethodName && m.Parameters.Length == 0);

        [MustUseReturnValue]
        public ICallable<Value> RequestPause()
        {
            return Get(ourMakePauseMethodFilter);
        }

        public static UnityPausePointHelper CreateHelper(IStackFrame frame, IValueFetchOptions options,
            IKnownTypes<Value> knownTypes, string assemblyLocation)
        {
           return CreateUnityDebuggerHelper<UnityPausePointHelper>(frame, options, knownTypes, assemblyLocation, AssemblyName,
                RequiredType, (reifiedType, domainTypes) => new UnityPausePointHelper(reifiedType, domainTypes));
        }
    }
}