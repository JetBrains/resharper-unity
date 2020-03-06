using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityController : IUnityController
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;
        private readonly Lifetime myLifetime;

        private FileSystemPath EditorInstanceJsonPath => mySolution.SolutionDirectory.Combine("Library/EditorInstance.json");

        public UnityController(UnityEditorProtocol unityEditorProtocol, ISolution solution, ILogger logger, Lifetime lifetime)
        {
            myUnityEditorProtocol = unityEditorProtocol;
            mySolution = solution;
            myLogger = logger;
            myLifetime = lifetime;
        }
        
        public Task<bool> ExitUnityAsync(bool force)
        {
            var lifetimeDef = myLifetime.CreateNested();
            if (myUnityEditorProtocol.UnityModel.Value == null) // no connection
            {
                if (force)
                {
                    return Task.FromResult(KillProcess());
                }
            }
            else
            {
                var protocolTask = myUnityEditorProtocol.UnityModel.Value.ExitUnity.Start(lifetimeDef.Lifetime, Unit.Instance).AsTask();
                var waitTask = Task.WhenAny(protocolTask, TaskEx.Delay(TimeSpan.FromSeconds(1))); // continue on timeout
                waitTask.ContinueWith(t =>
                {
                    lifetimeDef.Terminate();
                    if (protocolTask.Status != TaskStatus.RanToCompletion && force)
                        return KillProcess();
                    return false;
                }, myLifetime);                
            }
            return Task.FromResult(false);
        }

        // todo: remove
        public bool ExitUnity(bool force)
        {
            var task = ExitUnityAsync(force);
            task.Wait(myLifetime);
            return task.Result;
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
            {
                unityPath = EditorInstanceJson.TryGetValue(EditorInstanceJsonPath, "app_path");
                if (unityPath != null && PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX)
                    unityPath = FileSystemPath.Parse(unityPath).Combine("Contents/MacOS/Unity").FullPath;
            }
            
            return unityPath != null ? new[] {unityPath, "-projectPath", mySolution.SolutionDirectory.FullPath} : null;
        }

        public bool IsUnityGeneratedProject(IProject project)
        {
            return project.IsUnityGeneratedProject();
        }


        private bool KillProcess()
        {
            try
            {
                var possibleProcessId = TryGetUnityProcessId();
                if (possibleProcessId > 0)
                {
                    Process process = null;
                    try
                    {
                        process = Process.GetProcessById((int)possibleProcessId);
                    }
                    catch (Exception)
                    {
                        // process may not be running
                    }

                    if (process != null)
                    {
                        process.Kill();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
            }

            return false;
        }
    }
}