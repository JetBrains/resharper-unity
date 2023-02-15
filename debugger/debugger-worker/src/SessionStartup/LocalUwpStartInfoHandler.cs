using System;
using System.Diagnostics;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.Util;
using JetBrains.Util.Utils;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.Connections.StartArgs;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    // UWP processes are not allowed to accept incoming socket connections from localhost by default (apparently as part
    // of a sandbox for Windows Store apps - they are isolated from each other and from communicating with each other
    // except via system provided contracts). We can use the CheckNetIsolation.exe util to temporarily enable this.
    // We don't need to do this for remote connections as these are allowed via the manifest which Unity creates.
    // See also:
    // * https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/loopback
    // * https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-firewall/troubleshooting-uwp-firewall#debugging-uwp-app-loopback-scenarios
    [DebuggerGlobalComponent]
    public class LocalUwpStartInfoHandler : UnityStartInfoHandlerBase<UnityLocalUwpStartInfo>
    {
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;

        public LocalUwpStartInfoHandler(Lifetime lifetime, ILogger logger)
            : base(SoftDebuggerType.Instance)
        {
            myLifetime = lifetime;
            myLogger = logger;
        }

        protected override IDebuggerSessionStarter GetSessionStarter(UnityLocalUwpStartInfo localUwpStartInfo,
                                                                     IDebuggerSessionOptions debuggerSessionOptions)
        {
            var softDebuggerStartInfo = CreateSoftDebuggerStartInfo(localUwpStartInfo);
            return new LocalUwpSessionStarter(myLifetime, localUwpStartInfo, softDebuggerStartInfo,
                debuggerSessionOptions,
                myLogger);
        }

        private class LocalUwpSessionStarter : DebuggerSessionStarterBase
        {
            private readonly Lifetime myLifetime;
            private readonly UnityLocalUwpStartInfo myLocalUwpStartInfo;
            private readonly SoftDebuggerStartArgs mySoftDebuggerStartInfo;
            private readonly ILogger myLogger;

            public LocalUwpSessionStarter(Lifetime lifetime,
                                          UnityLocalUwpStartInfo localUwpStartInfo,
                                          SoftDebuggerStartArgs softDebuggerStartInfo,
                                          IDebuggerSessionOptions evaluationOptions,
                                          ILogger logger)
                : base(evaluationOptions)
            {
                myLifetime = lifetime;
                myLocalUwpStartInfo = localUwpStartInfo;
                mySoftDebuggerStartInfo = softDebuggerStartInfo;
                myLogger = logger;
            }

            protected override void DoStartSession(IDebuggerSession session, IDebuggerSessionOptions options)
            {
                StartCheckNetIsolation(session);
                session.Run(mySoftDebuggerStartInfo, options);
            }

            private void StartCheckNetIsolation(IDebuggerSession session)
            {
                if (!PlatformUtil.IsRunningUnderWindows)
                    return;

                try
                {
                    // The UAC prompt shows "AppContainer Network Isolation Diagnostic Tool"
                    // WriteLine will appear in the debugger's Console pane
                    session.LogWriter?.Invoke(false, "Starting 'AppContainer Network Isolation Diagnostic Tool' (CheckNetIsolation.exe)");
                    var verb = PlatformUtilWindows.IsRunningElevated ? "open" : "runas";
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "CheckNetIsolation.exe",
                        Arguments = $"LoopbackExempt -is -n={myLocalUwpStartInfo.PackageName}",
                        UseShellExecute = true,
                        Verb = verb,
                        WindowStyle = ProcessWindowStyle.Minimized
                    };
                    var process = Process.Start(processStartInfo);
                    if (process != null)
                        myLifetime.OnTermination(() => StopCheckNetIsolation(process, session));
                }
                catch (Exception e)
                {
                    session.LogWriter?.Invoke(true, $"Error running CheckNetIsolation.exe: {e.Message}");
                    myLogger.Error(e);
                    throw;
                }
            }

            private void StopCheckNetIsolation(Process process, IDebuggerSession session)
            {
                session.LogWriter?.Invoke(false, "Stopping 'AppContainer Network Isolation Diagnostic Tool'");
                process.Kill();
                process.WaitForExit();
                process.Close();
            }
        }
    }
}