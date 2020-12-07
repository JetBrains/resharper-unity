using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.Tasks;
using JetBrains.Rd.Base;
using JetBrains.Util;
using JetBrains.Util.Reflection;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    public interface IUnityReferenceChangeHandler
    {
        // This is guaranteed to be called on all handlers before any handler receives OnUnityProjectAdded
        void OnHasUnityReference();
        void OnUnityProjectAdded(Lifetime projectLifetime, IProject project);
    }

    [SolutionComponent]
    public class UnityReferencesTracker : IChangeProvider
    {
        // Unity 2017.3 split UnityEngine into modules. The copy in the Managed folder is the original monolithic build.
        // The Managed/UnityEngine/ folder contains the version split into modules, and generated projects reference the
        // UnityEngine.dll in this folder, as well as the modules. Managed/UnityEngine/UnityEngine.dll has a load of
        // type forwards to the new modules. Non-generated/manually maintained projects can still reference the original
        // Managed/UnityEngine.dll, and the type forwards will fix things up at runtime.
        // We check for references to UnityEngine.dll, UnityEngine.CoreModule.dll and (just in case)
        // UnityEngine.ShaderInternalsModule.dll
        // Unity 2020.2 similarly splits UnityEditor.dll, primarily to allow packages to override implementations, such
        // as UIElements.
        private static readonly JetHashSet<string> ourUnityReferenceNames = new JetHashSet<string>
        {
            "UnityEngine",
            "UnityEngine.CoreModule",
            "UnityEngine.SharedInternalsModule",
            "UnityEditor",
            "UnityEditor.CoreModule"
        };

        private static readonly ICollection<AssemblyNameInfo> ourUnityReferenceNameInfos;

        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly ModuleReferenceResolveSync myModuleReferenceResolveSync;
        private readonly ChangeManager myChangeManager;
        private readonly IViewableProjectsCollection myProjects;
        private readonly ICollection<IUnityReferenceChangeHandler> myHandlers;
        private readonly Dictionary<IProject, Lifetime> myAllProjectLifetimes;
        private readonly HashSet<IProject> myUnityProjects;

        // If you only want to be notified that we're a Unity solution, advise this.
        // If all you're interested in is being notified that we're a Unity solution, advise this. If you need to know
        // we're a Unity solution *and*/or know about Unity projects (and get a per-project lifetime), implement
        // IUnityReferenceChangeHandler
        public readonly ViewableProperty<bool> HasUnityReference = new ViewableProperty<bool>(false);

        static UnityReferencesTracker()
        {
            ourUnityReferenceNameInfos = new List<AssemblyNameInfo>();
            foreach (var name in ourUnityReferenceNames)
                ourUnityReferenceNameInfos.Add(AssemblyNameInfoFactory.Create2(name, null));
        }

        public UnityReferencesTracker(
            Lifetime lifetime,
            IEnumerable<IUnityReferenceChangeHandler> handlers,
            ISolution solution,
            ISolutionLoadTasksScheduler scheduler,
            ModuleReferenceResolveSync moduleReferenceResolveSync,
            ChangeManager changeManager,
            IViewableProjectsCollection projects,
            ILogger logger,
            UnityVersion unityVersion)
        {
            myAllProjectLifetimes = new Dictionary<IProject, Lifetime>();
            myUnityProjects = new HashSet<IProject>();

            myHandlers = handlers.ToList();
            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myModuleReferenceResolveSync = moduleReferenceResolveSync;
            myChangeManager = changeManager;
            myProjects = projects;

            // At PreparePsiModules, we know what references we have, so we know if we're a Unity project. This is where
            // we'll initialise our custom PSI module. We have to initialise our PSI module before Done, or the
            // PersistentIndexManager will clean out the "orphaned" external (YAML) files and we'll have to reparse all
            // files on every startup
            scheduler.EnqueueTask(new SolutionLoadTask("Preparing Unity project", SolutionLoadTaskKinds.PreparePsiModules,
                OnSolutionPreparePsiModules));
            
            HasUnityReference.WhenTrue(lifetime, lt => unityVersion.ActualVersionForSolution.Set(unityVersion.GetActualVersionForSolution()));
        }

        private void OnSolutionPreparePsiModules()
        {
            myChangeManager.RegisterChangeProvider(myLifetime, this);
            myChangeManager.AddDependency(myLifetime, this, myModuleReferenceResolveSync);

            // Track the lifetime of all projects, so we can pass it to the handler later
            myProjects.Projects.View(myLifetime,
                (projectLifetime, project) =>
                {
                    myAllProjectLifetimes.Add(projectLifetime, project, projectLifetime);
                    if (HasUnityReferenceOrFlavour(project))
                        myUnityProjects.Add(projectLifetime, project);
                });

            var unityProjectLifetimes = myAllProjectLifetimes.Where(pair => HasUnityReferenceOrFlavour(pair.Key)).ToList();
            if (unityProjectLifetimes.Count == 0)
                return;

            NotifyHasUnityReference();
            NotifyOnUnityProjectAdded(unityProjectLifetimes);
        }

        private void NotifyHasUnityReference()
        {
            if (!HasUnityReference.Value)
            {
                HasUnityReference.SetValue(true);
                foreach (var handler in myHandlers) handler.OnHasUnityReference();
            }
        }

        private void NotifyOnUnityProjectAdded(List<KeyValuePair<IProject, Lifetime>> unityProjectLifetimes)
        {
            foreach (var handler in myHandlers)
            {
                foreach (var (project, lifetime) in unityProjectLifetimes)
                {
                   handler.OnUnityProjectAdded(lifetime, project);
                }
            }
        }

        object IChangeProvider.Execute(IChangeMap changeMap)
        {
            var projectModelChange = changeMap.GetChange<ProjectModelChange>(mySolution);
            if (projectModelChange == null)
                return null;

            var changes = ReferencedAssembliesService.TryGetAssemblyReferenceChanges(projectModelChange,
                ourUnityReferenceNameInfos, myLogger.Trace());

            var newUnityProjects = new List<KeyValuePair<IProject, Lifetime>>();
            foreach (var change in changes)
            {
                if (change.IsAdded)
                {
                    var project = change.GetNewProject();
                    if (HasUnityReferenceOrFlavour(project))
                    {
                        Assertion.Assert(myAllProjectLifetimes.ContainsKey(project), "project is not added");
                        if (myAllProjectLifetimes.TryGetValue(project, out var projectLifetime))
                        {
                            newUnityProjects.Add(JetKeyValuePair.Of(project, projectLifetime));
                            if (!myUnityProjects.Contains(project))
                                myUnityProjects.Add(projectLifetime, project);
                        }
                    }
                }
            }

            if (newUnityProjects.Count > 0)
            {
                myChangeManager.ExecuteAfterChange(() =>
                {
                    NotifyHasUnityReference();
                    NotifyOnUnityProjectAdded(newUnityProjects);
                });
            }

            return null;
        }

        public bool IsUnityProject(IProject project)
        {
            return myUnityProjects.Contains(project);
        }

        private static bool HasUnityReferenceOrFlavour([NotNull] IProject project)
        {
            return project.HasUnityFlavour() || ReferencesUnity(project);
        }

        public static bool ReferencesUnity(IProject project)
        {
            var targetFrameworkId = project.GetCurrentTargetFrameworkId();
            foreach (var reference in project.GetModuleReferences(targetFrameworkId))
            {
                if (ourUnityReferenceNames.Contains(reference.Name))
                    return true;
            }
            return false;
        }
    }
}