using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Lifetimes;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Exceptions
{
    [DebuggerSessionComponent]
    public class UnityUnhandledExceptionHandler : IDebuggerStartable
    {
        private readonly IDebuggerSession mySession;
        private readonly Lifetime myLifetime;
        private readonly bool myIsUnitySession;

        public UnityUnhandledExceptionHandler(IDebuggerSession session, Lifetime lifetime, ISessionCreationInfo creationInfo)
        {
            mySession = session;
            myLifetime = lifetime;
            myIsUnitySession = creationInfo.StartInfo is UnityStartInfo;
        }

        public void Start()
        {
            if (!myIsUnitySession) return;

            mySession.TargetReady += (_, _) =>
            {
                // Unity uses ExitGUIException for control flow in its IMGUI system - thrown in managed code and caught in native code.
                // It is thrown rather often while using the Inspector and some other (see uses of ExitGUI in the reference source).
                // Since it's not _really_ an exception and it would be annoying to break every time user presses "Add Component",
                // the best we can do is to simply ignore it. Hence we create the catchpoint with no action.
                mySession.BreakpointsManager.AddCatchpoint(myLifetime, "UnityEngine.ExitGUIException",
                    includeSubclasses: false,
                    breakIfThrownInUserCode: true,
                    breakIfThrownInExternalCode: true,
                    breakIfHandledByUserCode: true,
                    breakIfHandledByOtherCode: true,
                    breakIfUnhandled: true,
                    beforeInsert: static catchpoint => catchpoint.HitAction = HitAction.None,
                    statusChangedHandler: null
                );
            };
        }
    }
}