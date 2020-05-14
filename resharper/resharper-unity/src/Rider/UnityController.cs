using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityController : IUnityController
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;
        private readonly ISolution mySolution;
        private readonly Lifetime myLifetime;
        private readonly UnitySolutionTracker mySolutionTracker;
        private RdUnityModel myRdUnityModel;

        private FileSystemPath EditorInstanceJsonPath => mySolution.SolutionDirectory.Combine("Library/EditorInstance.json");

        public UnityController(UnityEditorProtocol unityEditorProtocol, ISolution solution,
            Lifetime lifetime, UnitySolutionTracker solutionTracker)
        {
            if (solution.GetData(ProjectModelExtensions.ProtocolSolutionKey) == null)
                return;
            
            myUnityEditorProtocol = unityEditorProtocol;
            mySolution = solution;
            myLifetime = lifetime;
            mySolutionTracker = solutionTracker;
            myRdUnityModel = solution.GetProtocolSolution().GetRdUnityModel();
        }

        public Task<ExitUnityResult> ExitUnityAsync(bool force)
        {
            var lifetimeDef = myLifetime.CreateNested();
            if (myUnityEditorProtocol.UnityModel.Value == null) // no connection
            {
                if (force)
                {
                    return Task.FromResult(KillProcess());
                }

                return Task.FromResult(new ExitUnityResult(false, "No connection to Unity Editor.", null));
            }

            var protocolTask = myUnityEditorProtocol.UnityModel.Value.ExitUnity.Start(lifetimeDef.Lifetime, Unit.Instance).AsTask();
            var waitTask = Task.WhenAny(protocolTask, Task.Delay(TimeSpan.FromSeconds(0.5), lifetimeDef.Lifetime)); // continue on timeout
            return waitTask.ContinueWith(t =>
            {
                lifetimeDef.Terminate();
                if (protocolTask.Status != TaskStatus.RanToCompletion && force)
                    return KillProcess();
                return new ExitUnityResult(false, "Attempt to close Unity Editor failed.", null);
            }, TaskContinuationOptions.AttachedToParent);
        }

        public int? TryGetUnityProcessId()
        {
            var model = myUnityEditorProtocol.UnityModel.Value;
            if (model != null)
            {
                if (model.UnityProcessId.HasValue())
                {
                    return model.UnityProcessId.Value;    
                }
            }
            // no protocol connection - try to fallback to EditorInstance.json
            var processIdString = EditorInstanceJson.TryGetValue(EditorInstanceJsonPath, "process_id");
            return processIdString == null ? (int?) null : Convert.ToInt32(processIdString);
        }

        [CanBeNull]
        public string[] GetUnityCommandline()
        {
            var unityPathData = myUnityEditorProtocol.UnityModel.Value?.UnityApplicationData;
            if (!unityPathData.HasValue()) 
                return null;
            
            var unityPath = unityPathData?.Value?.ApplicationPath;

            if (unityPath == null)
                unityPath = EditorInstanceJson.TryGetValue(EditorInstanceJsonPath, "app_path");
            
            if (unityPath != null && PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX)
                unityPath = FileSystemPath.Parse(unityPath).Combine("Contents/MacOS/Unity").FullPath;
            
            return unityPath != null ? new[] {unityPath, "-projectPath", mySolution.SolutionDirectory.FullPath} : null;
        }

        public bool IsUnityGeneratedProject(IProject project)
        {
            return project.IsUnityGeneratedProject();
        }

        public bool IsUnityEditorUnitTestRunStrategy(IUnitTestRunStrategy strategy)
        {
            return UnityNUnitServiceProvider.IsUnityUnitTestStrategy(mySolutionTracker, myRdUnityModel, myUnityEditorProtocol);
        }

        private ExitUnityResult KillProcess()
        {
            ExitUnityResult result = null;
            try
            {
                var possibleProcessId = TryGetUnityProcessId();
                if (possibleProcessId > 0)
                {
                    Process process = null;
                    try
                    {
                        process = Process.GetProcessById((int) possibleProcessId);
                    }
                    catch (Exception)
                    {
                        // process may not be running
                    }

                    if (process != null)
                    {
                        process.Kill();
                        result = new ExitUnityResult(true, null, null);
                    }
                }
            }
            catch (Exception e)
            {
                result = new ExitUnityResult(false, "Exception on attempt to kill Unity Editor process", e);
            }

            return result;
        }
    }
}