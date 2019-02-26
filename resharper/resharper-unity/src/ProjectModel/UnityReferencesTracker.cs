using System;
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
        private readonly Dictionary<IProject, Lifetime> myProjectLifetimes;

        // If you want to be notified that we're a Unity solution, advise this. If you need a per-project lifetime,
        // implement IUnityReferenceChangeHandler
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
            myProjectLifetimes = new Dictionary<IProject, Lifetime>();

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
                (projectLifetime, project) => myProjectLifetimes.Add(project, projectLifetime));

            var unityProjectLifetimes = myProjectLifetimes.Where(pl => pl.Key.IsUnityProject()).ToList();

            if (unityProjectLifetimes.Any())
                HasUnityReference.SetValue(true);

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

            var newUnityProjects = new JetHashSet<IProject>();
            foreach (var change in changes)
            {
                if (change.IsAdded)
                {
                    var project = change.GetNewProject();
                    if (project.IsUnityProject())
                         newUnityProjects.Add(project);
                }
            }

            myChangeManager.ExecuteAfterChange(() =>
            {
                foreach (var project in newUnityProjects)
                    OnUnityProjectAdded(project);
            });

            return null;
        }

        private void OnUnityProjectAdded(IProject project)
        {
            if (!myProjectLifetimes.TryGetValue(project, out var projectLifetime))
                return;

            if (!HasUnityReference.Value)
                HasUnityReference.SetValue(true);

            var exceptions = new LocalList<Exception>();
            foreach (var handler in myHandlers)
            {
                try
                {
                    handler.OnUnityProjectAdded(projectLifetime, project);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException("Failed to handle project changes", exceptions.ToArray());
        }
    }
}