#nullable enable
using System;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Core;
using JetBrains.IDE.StackTrace;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Nodes;
using JetBrains.ReSharper.Feature.Services.StackTraces.StackTrace.Parsers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Backend.Features.StackTrace;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProfilerEventsHost(
    ILogger logger,
    ISolution solution)
{
    public void AdviseOpenFileByMethodName(UnityProfilerModel unityProfilerModel, FrontendBackendHost frontendBackendHost)
    {
        unityProfilerModel.OpenFileBySampleInfo.SetAsync((_, sampleStackInfo) =>
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
                    logger.Error(e);
                    result.Set(Unit.Instance);
                }

                try
                {
                    ShowConsoleWithCallstack(frontendBackendHost, sampleStackInfo.SampleStack, sampleStackInfo.SampleName);
                }
                catch (Exception e)
                {
                    logger.Error(e);
                    result.Set(Unit.Instance);
                }
            }

            return result;
        });
    }

    private void ShowConsoleWithCallstack(FrontendBackendHost frontendBackendHost, string sampleStack, string sampleName)
    {
        frontendBackendHost.Do(model => { model.ActivateRider(); });
        sampleStack = sampleStack.Replace("/", "\n");
        solution.GetComponent<RiderStackTraceHost>().ShowConsole(new StackTraceConsole(sampleName, sampleStack));
    }

    private void NavigateToCode(string profilerCallstack)
    {
        var stackTraceOptions = solution.GetComponent<StackTraceOptions>();
        var parser = new StackTraceParser(profilerCallstack, solution,
            solution.GetComponent<StackTracePathResolverCache>(), stackTraceOptions.GetState());

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
                occurrence.Navigate(solution,
                    solution.GetComponent<IMainWindowPopupWindowContext>().Source, true);
            else
            {
                logger.Verbose($"No occurrence found for '{profilerCallstack}'");
            }
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }
}