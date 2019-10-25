using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    internal class UnityRiderUnitTestAlternateProjectOutputProvider : DefaultRiderUnitTestAlternateProjectOutputProvider
    {
        private readonly IShellLocks myShellLocks;

        private volatile IDictionary<string, FileSystemPath> myProjectNameToOutputFilePathMap;

        public UnityRiderUnitTestAlternateProjectOutputProvider(Lifetime lifetime, UnityEditorProtocol editorProtocol, IShellLocks shellLocks) : base(lifetime)
        {
            myShellLocks = shellLocks;

            myProjectNameToOutputFilePathMap = new ConcurrentDictionary<string, FileSystemPath>();

            editorProtocol.UnityModel.ViewNotNull(lifetime, (modelLifetime, model) =>
            {
                model.CompiledAssemblies.AdviseNotNull(modelLifetime, compiledAssemblies =>
                {
                    myProjectNameToOutputFilePathMap = compiledAssemblies.ToDictionary(a => a.Name, a => FileSystemPath.TryParse(a.OutputPath));
                    PathsChanged.Fire();
                });
            });
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