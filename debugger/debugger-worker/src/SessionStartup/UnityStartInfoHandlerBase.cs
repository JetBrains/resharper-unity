using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using JetBrains.Debugger.Worker.Mono;
using JetBrains.Debugger.Worker.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.DebuggerWorker;
using JetBrains.Rider.Model.Unity.DebuggerWorker;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    public abstract class UnityStartInfoHandlerBase<T> : ModelStartInfoHandlerBase<T>
        where T : UnityStartInfoBase
    {
        protected UnityStartInfoHandlerBase(DebuggerType debuggerType)
            : base(debuggerType)
        {
        }

        // We have to inject this or we get a circular reference - options depends on DebuggerWorker which depends on
        // start info handlers
        [Injected] internal IUnityOptions UnityOptions { get; set; }

        protected override IDebuggerSessionOptions CreateSessionOptions(Lifetime lifetime, T startInfo,
                                                                        SessionProperties properties)
        {
            return new Il2CppAwareSessionOptions(base.CreateSessionOptions(lifetime, startInfo, properties), UnityOptions);
        }

        protected override IDebuggerSessionStarter GetSessionStarter(T unityStartInfo,
                                                                     IDebuggerSessionOptions debuggerSessionOptions)
        {
            // ModelStartInfoHandlerBase will call CreateSessionOptions and immediately pass that into GetSessionStarter
            // so this is a safe cast
            return new MySessionStarter(CreateSoftDebuggerStartInfo(unityStartInfo),
                (Il2CppAwareSessionOptions) debuggerSessionOptions);
        }

        protected static SoftDebuggerStartInfo CreateSoftDebuggerStartInfo(UnityStartInfoBase startInfo)
        {
            var address = IPAddress.Loopback;
            var monoAddress = startInfo.MonoAddress;
            if (monoAddress != null)
            {
                try
                {
                    address = Dns.GetHostAddresses(monoAddress)
                        .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                }
                catch (Exception e)
                {
                    throw new DebuggerStartupException(e.Message, e);
                }

                if (address == null)
                {
                    throw new DebuggerStartupException(
                        "Host " + monoAddress + " cannot be resolved into any IP address");
                }
            }

            return new SoftDebuggerStartInfo((startInfo.ListenForConnections
                    ? (SoftDebuggerStartArgs) new SoftDebuggerListenArgs(string.Empty, address, startInfo.MonoPort)
                    : new SoftDebuggerConnectArgs(string.Empty, address, startInfo.MonoPort))
                .SetConnectionProperties());
        }

        private class Il2CppAwareSessionOptions : DelegatingDebuggerSessionOptions
        {
            private readonly IUnityOptions myUnityOptions;
            [CanBeNull] private SoftDebuggerSession mySoftDebuggerSession;
            private bool? myIsIl2Cpp;

            public Il2CppAwareSessionOptions(IDebuggerSessionOptions debuggerSessionOptionsImplementation,
                                             IUnityOptions unityOptions)
                : base(debuggerSessionOptionsImplementation)
            {
                myUnityOptions = unityOptions;
            }

            public void SetDebuggerSession(SoftDebuggerSession softDebuggerSession)
            {
                mySoftDebuggerSession = softDebuggerSession;
            }

            public override bool BreakOnUnhandledExceptions
            {
                get
                {
                    // Unity's Mono 4.x runtime does not signal unhandled exceptions, but IL2CPP does, which means we
                    // get unexpected exceptions in IL2CPP players vs Mono players/editor. Also, since IL2CPP doesn't
                    // always have debugging information (e.g. code converted from DLLs as assets) we don't always have
                    // call stacks. This all makes it look like Rider's debugger can't handle IL2CPP properly, even
                    // though it's actually working correctly. We override the user's break on exception settings if the
                    // VM reports itself as IL2CPP. We'll respect their settings for Mono (although Unity's 4.x does not
                    // signal unhandled exceptions)
                    if (myUnityOptions.IgnoreBreakOnUnhandledExceptionsForIl2Cpp)
                    {
                        if (myIsIl2Cpp.HasValue && myIsIl2Cpp.Value) return false;

                        // E.g. "mono 0.0 (IL2CPP)"
                        myIsIl2Cpp = mySoftDebuggerSession?.VirtualMachine?.Version?.VMVersion?.Contains("IL2CPP");
                    }

                    return DebuggerSessionOptionsImplementation.BreakOnUnhandledExceptions;
                }
            }
        }

        private class MySessionStarter : IDebuggerSessionStarter
        {
            private readonly DebuggerStartInfo myDebuggerStartInfo;
            private readonly Il2CppAwareSessionOptions myDebuggerSessionOptions;

            public MySessionStarter(DebuggerStartInfo debuggerStartInfo, Il2CppAwareSessionOptions debuggerSessionOptions)
            {
                myDebuggerStartInfo = debuggerStartInfo;
                myDebuggerSessionOptions = debuggerSessionOptions;
            }

            public void StartSession(IDebuggerSession session)
            {
                myDebuggerSessionOptions?.SetDebuggerSession(session as SoftDebuggerSession);
                session.Run(myDebuggerStartInfo, myDebuggerSessionOptions);
            }

            public IDebuggerSessionOptions Options => myDebuggerSessionOptions;
        }
    }
}