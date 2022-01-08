using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Debugger.Common.OutputAssemblies;
using JetBrains.Application.Infra;
using JetBrains.Debugger.Host.DebuggerOutputAssemblies;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Backend.Features.Debugger.Utils;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Host
{
    // Unity sets up the C# projects to build to Temp\Bin\Debug, but doesn't use these at runtime. It compiles its own
    // versions to Library/ScriptAssemblies, optionally post-processing/IL rewriting, and loads them from here. This can
    // affect the debugger's "user code" handling - it can mark user code as external code. The debugger reads metadata
    // from assemblies in the project's output folder, and this is enough to recognise the assemblies that Unity
    // compiles and loads from another location (based on assembly name, version, culture, etc.)
    // But if the C# project has not been built, the debugger can't read the metadata and can only go by name. This
    // isn't enough to match the Unity assemblies, and they are marked as external code, and can be hidden in the stack
    // Note that this doesn't affect player debugging, as remote debugging doesn't seem to get the required assembly
    // details
    [SolutionComponent]
    public class UnityDebuggerOutputAssembliesProviderHost : IDebuggerOutputAssembliesProvider
    {
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;

        public UnityDebuggerOutputAssembliesProviderHost(ISolution solution, ILogger logger)
        {
            mySolution = solution;
            myLogger = logger;
        }

        public Task<IReadOnlyList<DebuggerOutputAssemblyInfo>> GetOutputAssembliesInfoAsync(Lifetime lifetime)
        {
            if (!mySolution.HasUnityReference())
            {
                return Task.FromResult<IReadOnlyList<DebuggerOutputAssemblyInfo>>(
                    EmptyList<DebuggerOutputAssemblyInfo>.Instance);
            }

            return lifetime.StartBackgroundRead(() =>
                (IReadOnlyList<DebuggerOutputAssemblyInfo>) GetOutputAssembliesInfoInternal()
                    .ToList());
        }

        private IEnumerable<DebuggerOutputAssemblyInfo> GetOutputAssembliesInfoInternal()
        {
            var assemblyInfoDatabase = mySolution.GetComponent<AssemblyInfoDatabase>();
            foreach (var project in mySolution.GetAllProjects().Where(x => x.IsProjectFromUserView()))
            {
                var configurations = project.ProjectProperties.ActiveConfigurations;
                foreach (var targetFrameworkId in project.TargetFrameworkIds)
                {
                    var pathMap = configurations.TryGetPathMap(targetFrameworkId);
                    var outputAssemblyName = project.GetOutputFilePath(targetFrameworkId).Name;
                    var projectLocation = project.ProjectFileLocation.Parent;
                    var unityOutputPath =
                        projectLocation.Combine("Library/ScriptAssemblies").Combine(outputAssemblyName);
                    if (!unityOutputPath.IsEmpty && unityOutputPath.IsAbsolute)
                    {
                        var assemblyNameInfo = assemblyInfoDatabase.GetAssemblyName(unityOutputPath);
                        if (assemblyNameInfo.IsNullOrEmpty())
                        {
                            // The file should always exist - Unity will make sure it's there, as long as there are no
                            // compile errors. And if there are compile errors (or the file doesn't exist for other
                            // reasons), then debugging is unlikely to be successful, so there's nothing useful we can
                            // do here
                            myLogger.Warn("Cannot create assembly name for {0}", unityOutputPath);
                            continue;
                        }

                        yield return new DebuggerOutputAssemblyInfo(assemblyNameInfo, projectLocation.FullPath,
                            unityOutputPath.FullPath, in pathMap);
                    }
                }
            }
        }
    }
}