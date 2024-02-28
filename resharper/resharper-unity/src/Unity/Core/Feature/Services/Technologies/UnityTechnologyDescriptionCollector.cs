using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies
{
    [SolutionComponent(InstantiationEx.ContainerAsyncAnyThreadSafe)]
    public class UnityTechnologyDescriptionCollector
    {
        private readonly ISolutionLoadTasksScheduler myTasksScheduler;
        private readonly PackageManager myPackageManager;
        private readonly List<IUnityTechnologyDescription> myDescriptions;

        public IViewableMap<string, bool> DiscoveredTechnologies => myDiscoveredTechnologies;

        private ViewableMap<string, bool> myDiscoveredTechnologies = new ViewableMap<string, bool>();

        private ViewableProperty<bool> myProjectsProcessed = new ViewableProperty<bool>(false);
        private ViewableProperty<bool> myPackagesProcessed = new ViewableProperty<bool>(false);

        public readonly ViewableProperty<bool> Ready = new ViewableProperty<bool>(false);

        public UnityTechnologyDescriptionCollector(Lifetime lifetime, ISolution solution, ISolutionLoadTasksScheduler tasksScheduler, 
            UnitySolutionTracker solutionTracker, PackageManager packageManager)
        {
            myTasksScheduler = tasksScheduler;
            myPackageManager = packageManager;
            myDescriptions = new List<IUnityTechnologyDescription>()
            {
                new HDRPUnityTechnologyDescription(),
                new CoreRPUnityTechnologyDescription(),
                new URPUnityTechnologyDescription(),
                new EntitiesUnityTechnologyDescription(),
                new InputSystemUnityTechnologyDescription(),
                new BurstUnityTechnologyDescription(),
                new OdinUnityTechnologyDescription(),
                new PhotonUnityTechnologyDescription(),
                new PeekUnityTechnologyDescription(),
                new UniRxUnityTechnologyDescription(),
                new UniTaskUnityTechnologyDescription(),
                new DOTweenTechnologyDescription(),
                new UnityTestFrameworkDescription(),
                new PythonScriptingUnityTechnologyDescription(),
                new AddressablesUnityTechnologyDescription(),
                new AndroidLogCatUnityTechnologyDescription(),
                new CodeCoverageUnityTechnologyDescription(),
                new UnityCollectionsUnityTechnologyDescription(),
                new EditorCoroutinesUnityTechnologyDescription(),
                new EntitiesGraphicsUnityTechnologyDescription(),
                new LocalizationUnityTechnologyDescription(),
                new MathematicsUnityTechnologyDescription(),
                new TransportUnityTechnologyDescription(),
                new UnityPhysicsUnityTechnologyDescription(),
                new HavokUnityTechnologyDescription(),
                new MlAgentsUnityTechnologyDescription(),
                new MultiplayerToolsTechnologyDescription(),
                new NetCodeUnityTechnologyDescription(),
                new NetCodeGameObjectsUnityTechnologyDescription(),
                new SerializationUnityTechnologyDescription(),
                new LoggingUnityTechnologyDescription(),
                new MemoryProfilerUnityTechnologyDescription(),
                new ProfilerAnalyzerUnityTechnologyDescription(),
                new ProfilingCoreUnityTechnologyDescription(),
                new CollabUnityTechnologyDescription(),
                new VisualScriptingUnityTechnologyDescription(),
                new HotReloadUnityTechnologyDescription()
            };
            
            myProjectsProcessed.Compose(lifetime, myPackagesProcessed, (a, b) => a && b).AdviseUntil(lifetime, v =>
            {
                if (v)
                {
                    Ready.Value = true;
                    return true;
                }

                return false;
            });
            
            foreach (var description in myDescriptions)
            {
                myDiscoveredTechnologies[description.Id] = false;
            }
            
            myTasksScheduler.EnqueueTask(new SolutionLoadTask(GetType(), "UnityTechnologyDescriptionCollector", SolutionLoadTaskKinds.Done,
                () =>
                {
                    if (!solutionTracker.HasUnityReference.HasTrueValue())
                    {
                        myProjectsProcessed.Value = true;
                        myPackagesProcessed.Value = true;
                        return;
                    }

                    myPackageManager.IsInitialUpdateFinished.AdviseUntil(lifetime, v =>
                    {
                        if (v)
                        {
                            try
                            {
                                foreach (var (_, data) in myPackageManager.Packages.ToList())
                                {
                                    ProcessPackage(data);
                                }
                            }
                            finally
                            {
                                myPackagesProcessed.Value = true;
                            }
                            return true;
                        }

                        return false;
                    });
                    

                    using (ReadLockCookie.Create())
                    {
                        var projects = solution.GetAllProjects();
                        try
                        {
                            foreach (var project in projects)
                            {
                                ProcessProject(project);
                            }
                        }
                        finally
                        {
                            myProjectsProcessed.Value = true;
                        }
                    }
                }));
        }

        private void ProcessProject(IProject project)
        {
            var targetFrameworkId = project.GetCurrentTargetFrameworkId();
            foreach (var projectToModuleReference in project.GetModuleReferences(targetFrameworkId))
            {
                if (projectToModuleReference is IProjectToAssemblyReference projectToAssemblyReference)
                {
                    ProcessAssemblyReference(projectToAssemblyReference);
                }

                if (projectToModuleReference is IProjectToProjectReference projectToProjectReference)
                {
                    ProcessProjectReference(projectToProjectReference);
                }
            }
        }

        private void ProcessAssemblyReference(IProjectToAssemblyReference projectToAssemblyReference)
        {
            foreach (var description in myDescriptions)
            {
                foreach (var assemblyName in description.GetPossibleAssemblyName())
                {
                    if (assemblyName.Equals(projectToAssemblyReference.Name))
                    {
                        myDiscoveredTechnologies[description.Id] = true;
                        return;
                    }
                }
            }
        }

        private void ProcessProjectReference(IProjectToProjectReference projectToProjectReference)
        {
            var name = projectToProjectReference.GetTargetProjectSearchDescriptor().ProjectFileLocation.NameWithoutExtension;
            foreach (var description in myDescriptions)
            {
                foreach (var projectName in description.GetPossibleProjectName())
                {
                    if (name.Equals(projectName))
                    {
                        myDiscoveredTechnologies[description.Id] = true;
                        return;
                    }
                }
            }
        }

        private void ProcessPackage(PackageData data)
        {
            foreach (var description in myDescriptions)
            {
                foreach (var projectName in description.GetPossiblePackageName())
                {
                    if (data.PackageDetails.CanonicalName.Equals(projectName))
                    {
                        myDiscoveredTechnologies[description.Id] = true;
                        return;
                    }
                }
            }
        }
    }
}