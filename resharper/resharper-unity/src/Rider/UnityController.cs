using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityController : IUnityController
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;

        public UnityController(UnityEditorProtocol unityEditorProtocol, ISolution solution, ILogger logger)
        {
            myUnityEditorProtocol = unityEditorProtocol;
            mySolution = solution;
            myLogger = logger;
        }
        
        public Task<bool> ExitUnityAsync(bool force)
        {
            if (!myUnityEditorProtocol.UnityModel.HasValue())
                return new Task<bool>(() => false);
            return myUnityEditorProtocol.UnityModel.Value.ExitUnity.Start(Unit.Instance).AsTask()
                .ContinueWith(t =>
                {
                    if (t.Result || force)
                        return new Task<bool>(() => true);
                    return new Task<bool>(() =>
                    {
                        var possibleProcess = TryGetUnityProcessId();
                        if (possibleProcess != null)
                        {
                            var processId = (int) possibleProcess;
                            if (processId > 0)
                            {
                                try
                                {
                                    Process.GetProcessById((int)possibleProcess).Kill();
                                }
                                catch (Exception e)
                                {
                                    myLogger.LogException(e);
                                    return false;
                                }
                                return true;
                            }
                        }

                        return false;
                    });

                }).Result;
        }

        // todo: remove
        public bool ExitUnity(bool force)
        {
            var task = ExitUnityAsync(force);
            task.Wait();
            return task.Result;
        }

        public int? TryGetUnityProcessId()
        {
            return myUnityEditorProtocol.UnityModel.Value?.UnityProcessId.Value;
        }

        public string[] GetUnityCommandline()
        {
            var unityPath = myUnityEditorProtocol.UnityModel.Value?.UnityApplicationData.Value?.ApplicationPath;
            return new[] {unityPath, "-projectPath", mySolution.SolutionDirectory.FullPath};
        }
    }
}