using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.Tasks;
using JetBrains.Rd.Base;
using JetBrains.Util;

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
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly ModuleReferenceResolveSync myModuleReferenceResolveSync;
        private readonly ChangeManager myChangeManager;
        private readonly IViewableProjectsCollection myProjects;
        private readonly ICollection<IUnityReferenceChangeHandler> myHandlers;
        private readonly Dictionary<IProject, Lifetime> myAllProjectLifetimes;

        // If you only want to be notified that we're a Unity solution, advise this.
        // If all you're interested in is being notified that we're a Unity solution, advise this. If you need to know
        // we're a Unity solution *and*/or know about Unity projects (and get a per-project lifetime), implement
        // IUnityReferenceChangeHandler
        public readonly ViewableProperty<bool> HasUnityReference = new ViewableProperty<bool>(false);

        public UnityReferencesTracker(
            Lifetime lifetime,

            IEnumerable<IUnityReferenceChangeHandler> handlers,
            ISolution solution,
            ISolutionLoadTasksScheduler scheduler,
            ModuleReferenceResolveSync moduleReferenceResolveSync,
            ChangeManager changeManager,
            IViewableProjectsCollection projects,
            ILogger logger)
        {
            myAllProjectLifetimes = new Dictionary<IProject, Lifetime>();

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
        }

        private void OnSolutionPreparePsiModules()
        {
            myChangeManager.RegisterChangeProvider(myLifetime, this);
            myChangeManager.AddDependency(myLifetime, this, myModuleReferenceResolveSync);

            // Track the lifetime of all projects, so we can pass it to the handler later
            myProjects.Projects.View(myLifetime,
                (projectLifetime, project) => myAllProjectLifetimes.Add(project, projectLifetime));

            var unityProjectLifetimes = myAllProjectLifetimes.Where(pair => pair.Key.IsUnityProject()).ToList();
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
                    handler.OnUnityProjectAdded(lifetime, project);
            }
        }

        object IChangeProvider.Execute(IChangeMap changeMap)
        {
            var projectModelChange = changeMap.GetChange<ProjectModelChange>(mySolution);
            if (projectModelChange == null)
                return null;

            var changes = ReferencedAssembliesService.TryGetAssemblyReferenceChanges(projectModelChange,
                ProjectExtensions.UnityReferenceNames, myLogger);

            var newUnityProjects = new List<KeyValuePair<IProject, Lifetime>>();
            foreach (var change in changes)
            {
                if (change.IsAdded)
                {
                    var project = change.GetNewProject();
                    if (project.IsUnityProject())
                    {
                        if (myAllProjectLifetimes.TryGetValue(project, out var projectLifetime))
                            newUnityProjects.Add(JetKeyValuePair.Of(project, projectLifetime));
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
    }
}