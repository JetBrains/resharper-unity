using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.Rider.Backend.Features.UnitTesting;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [SolutionComponent]
    internal class UnityRiderAlternateProjectOutputProvider : DefaultRiderAlternateProjectOutputProvider
    {
        private readonly IShellLocks myShellLocks;

        private volatile IDictionary<string, FileSystemPath> myProjectNameToOutputFilePathMap;

        public UnityRiderAlternateProjectOutputProvider(Lifetime lifetime, BackendUnityHost backendUnityHost,
                                                        IShellLocks shellLocks)
            : base(lifetime)
        {
            myShellLocks = shellLocks;

            myProjectNameToOutputFilePathMap = new ConcurrentDictionary<string, FileSystemPath>();

            backendUnityHost.BackendUnityModel.ViewNotNull(lifetime, (modelLifetime, backendUnityModel) =>
            {
                backendUnityModel.CompiledAssemblies.AdviseNotNull(modelLifetime, compiledAssemblies =>
                {
                    myProjectNameToOutputFilePathMap =
                        compiledAssemblies.ToDictionary(a => a.Name, a => FileSystemPath.TryParse(a.OutputPath));
                    PathsChanged.Fire();
                });
            });
        }

        public override bool ProjectCanHaveAlternateOutput(IProject project)
        {
            myShellLocks.AssertReadAccessAllowed();
            return project.IsUnityProject();
        }

        public override bool TryGetProjectOutputFilePath(IProject project, out FileSystemPath outputFilePath)
        {
            myShellLocks.AssertReadAccessAllowed();

            if (!project.IsUnityProject())
                return base.TryGetProjectOutputFilePath(project, out outputFilePath);

            return myProjectNameToOutputFilePathMap.TryGetValue(project.Name, out outputFilePath) && !outputFilePath.IsNullOrEmpty();
        }
    }
}