#nullable enable
using System;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.IDE.StackTrace;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Nodes;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Parsers;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Backend.Features.StackTrace;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;

[SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
public class UnityProfilerEventsHost : IUnityLazyComponent
{
    private readonly RiderStackTraceHost myRiderStackTraceHost;
    private readonly ILogger myLogger;
    private readonly ISolution mySolution;

    public UnityProfilerEventsHost(Lifetime lifetime,
        BackendUnityHost backendUnityHost, FrontendBackendHost frontendBackendHost,
        RiderStackTraceHost riderStackTraceHost, ILogger logger, ISolution solution)
    {
        myRiderStackTraceHost = riderStackTraceHost;
        myLogger = logger;
        mySolution = solution;
        if (!frontendBackendHost.IsAvailable) return;

        var frontendBackendModel = frontendBackendHost.Model;
        if (frontendBackendModel == null) return; // can only happen in tests
        backendUnityHost.BackendUnityModel.AdviseNotNull(lifetime, backendUnityModel =>
        {
            AdviseOpenFileByMethodName(backendUnityModel, frontendBackendModel);
        });
    }

    private void AdviseOpenFileByMethodName(BackendUnityModel backendUnityModel,
        FrontendBackendModel frontendBackendModel)
    {
        backendUnityModel.OpenFileBySampleInfo.SetAsync((_, sampleStackInfo) =>
        {
            var result = new RdTask<Unit>();
            if (string.IsNullOrEmpty(sampleStackInfo.SampleStack) || string.IsNullOrEmpty(sampleStackInfo.SampleName))
                return result;

            using (ReadLockCookie.Create())
            {
                try
                {
                    NavigateToCode(sampleStackInfo.SampleStack);
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    result.Set(Unit.Instance);
                }

                try
                {
                    ShowConsoleWithCallstack(frontendBackendModel, sampleStackInfo.SampleStack, sampleStackInfo.SampleName);
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    result.Set(Unit.Instance);
                }
            }

            return result;
        });
    }

    private void ShowConsoleWithCallstack(FrontendBackendModel frontendBackendModel, string sampleStack, string sampleName)
    {
        frontendBackendModel.ActivateRider();
        sampleStack = sampleStack.Replace("/", "\n");
        myRiderStackTraceHost.ShowConsole(new StackTraceConsole(sampleName, sampleStack));
    }

    private void NavigateToCode(string profilerCallstack)
    {
        var stackTraceOptions = mySolution.GetComponent<StackTraceOptions>();
        var parser = new StackTraceParser(profilerCallstack, mySolution,
            mySolution.GetComponent<StackTracePathResolverCache>(), stackTraceOptions.GetState());

        try
        {
            var rootNode = parser.Parse(0, profilerCallstack.Length);
            IOccurrence? occurrence = null;

            //Attempting to get the most bottom node
            //callstack example:
            // PlayerLoop
            // |- Update.ScriptRunBehaviourUpdate
            // |-- BehaviourUpdate
            // |--- Assembly-CSharp.dll!MyNamespace1::HeavyScript1.Update() [Invoke]
            foreach (var nodeNode in rootNode.Nodes)
            {
                if (nodeNode is IdentifierNode identifierNode)
                {
                    var identifierNodeResolveState = identifierNode.ResolveState;

                    using (CompilationContextCookie.GetExplicitUniversalContextIfNotSet())
                    {
                        occurrence = identifierNodeResolveState.MainCandidate?
                            .GetNavigationDeclarations().FirstOrDefault() ?? occurrence;
                    }
                }
            }

            if (occurrence != null)
                occurrence.Navigate(mySolution,
                    mySolution.GetComponent<IMainWindowPopupWindowContext>().Source, true);
            else
            {
                myLogger.Verbose($"No occurrence found for '{profilerCallstack}'");
            }
        }
        catch (Exception e)
        {
            myLogger.LogException(e);
        }
    }
}