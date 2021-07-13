using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.Exceptions;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Exceptions
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityUnhandledExceptionHandler : ISoftDebuggerUnhandledExceptionHandler
    {
        private readonly IUnityOptions myUnityOptions;

        public UnityUnhandledExceptionHandler(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        public bool ShouldContinueOnException(ObjectMirror exception)
        {
            if (!myUnityOptions.ExtensionsEnabled)
                return false;

            // Unity 2021.2 has an upgraded Mono (from 5.12ish to 6.something) and the behaviour around unhandled
            // exceptions has changed. Previously, Unity's Mono would not break on unhandled exceptions - it would not
            // notify the debugger. This appears to have been unintentional, and it's not currently clear if this
            // behaviour will be rolled back. (See also Il2CppAwareSessionOptions)
            // In the meantime, Unity uses ExitGUIException for control flow - thrown in managed code and caught in
            // native code. Mono reports this as an unhandled exception, because it is unhandled in managed code. But it
            // is used to break out of the immediate mode GUI loop, and is thrown frequently while using the Inspector
            // and other UI (see uses of ExitGUI in the reference source)
            return exception.Type.FullName == "UnityEngine.ExitGUIException";
        }
    }
}