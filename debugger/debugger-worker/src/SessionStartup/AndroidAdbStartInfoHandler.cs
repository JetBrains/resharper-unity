using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.Connections.StartArgs;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    [DebuggerGlobalComponent]
    public class AndroidAdbStartInfoHandler : UnityStartInfoHandlerBase<UnityAndroidAdbStartInfo>
    {
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;

        public AndroidAdbStartInfoHandler(Lifetime lifetime, ILogger logger)
            : base(SoftDebuggerType.Instance)
        {
            myLifetime = lifetime;
            myLogger = logger;
        }

        protected override IDebuggerSessionStarter GetSessionStarter(UnityAndroidAdbStartInfo androidAdbStartInfo,
                                                                     IDebuggerSessionOptions debuggerSessionOptions)
        {
            var softDebuggerStartInfo = CreateSoftDebuggerStartInfo(androidAdbStartInfo);
            return new AndroidAdbSessionStarter(myLifetime, androidAdbStartInfo, softDebuggerStartInfo,
                debuggerSessionOptions, myLogger);
        }

        private class AndroidAdbSessionStarter : DebuggerSessionStarterBase
        {
            private readonly Lifetime myLifetime;
            private readonly UnityAndroidAdbStartInfo myAndroidAdbStartInfo;
            private readonly SoftDebuggerStartArgs mySoftDebuggerStartInfo;
            private readonly ILogger myLogger;

            public AndroidAdbSessionStarter(Lifetime lifetime,
                                            UnityAndroidAdbStartInfo androidAdbStartInfo,
                                            SoftDebuggerStartArgs softDebuggerStartInfo,
                                            IDebuggerSessionOptions evaluationOptions,
                                            ILogger logger)
                : base(evaluationOptions)
            {
                myLifetime = lifetime;
                myAndroidAdbStartInfo = androidAdbStartInfo;
                mySoftDebuggerStartInfo = softDebuggerStartInfo;
                myLogger = logger;
            }

            protected override void DoStartSession(IDebuggerSession session, IDebuggerSessionOptions options)
            {
                ForwardPorts(session);
                session.Run(mySoftDebuggerStartInfo, options);
            }

            private void ForwardPorts(IDebuggerSession session)
            {
                try
                {
                    var adbPath = Path.Combine(myAndroidAdbStartInfo.AndroidSdkRoot, "SDK/platform-tools/adb");
                    if (!File.Exists(adbPath))
                    {
                        adbPath += ".exe";
                        if (!File.Exists(adbPath))
                        {
                            myLogger.Error("Cannot find adb");
                            return;
                        }
                    }

                    // Forward the ports
                    var arguments =
                        $"-s {myAndroidAdbStartInfo.AndroidDeviceId} forward tcp:{myAndroidAdbStartInfo.MonoPort} tcp:{myAndroidAdbStartInfo.MonoPort}";
                    InvokeAdb(adbPath, arguments, session);

                    myLifetime.OnTermination(() =>
                    {
                        var args =
                            $"-s {myAndroidAdbStartInfo.AndroidDeviceId} forward --remove tcp:{myAndroidAdbStartInfo.MonoPort}";
                        InvokeAdb(adbPath, args, session);
                    });
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    throw;
                }
            }

            private void InvokeAdb(string adbPath, string arguments, IDebuggerSession debuggerSession)
            {
                try
                {
                    debuggerSession.LogWriter?.Invoke(false, "adb " + arguments + Environment.NewLine);
                    var process = new Process { EnableRaisingEvents = true };
                    process.Exited += (_, _) =>
                    {
                        if (process.ExitCode != 0)
                            myLogger.Error(process.StandardError.ReadToEnd());
                    };
                    process.StartInfo = new ProcessStartInfo(adbPath, arguments)
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true
                    };
                    process.Start();
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    throw;
                }
            }
        }
    }
}