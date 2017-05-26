using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionComponent]
    public class ProjectReferenceChangeTracker : IChangeProvider
    {
        private readonly ISolution mySolution;
        private readonly ChangeManager myChangeManager;
        private readonly ConcurrentBag<Action<Lifetime, IProject>> myHandlers;
        private readonly Dictionary<IProject, Lifetime> myProjectLifetimes;

        public ProjectReferenceChangeTracker(
            Lifetime lifetime,
            ISolution solution,
            ModuleReferenceResolveSync moduleReferenceResolveSync,
            ChangeManager changeManager,
            IViewableProjectsCollection projects)
        {
            myHandlers = new ConcurrentBag<Action<Lifetime, IProject>>();
            myProjectLifetimes = new Dictionary<IProject, Lifetime>();
            
            mySolution = solution;
            myChangeManager = changeManager;
            myProjectLifetimes = new Dictionary<IProject, Lifetime>();
            
            myChangeManager.RegisterChangeProvider(lifetime, this);
            myChangeManager.AddDependency(lifetime, this, moduleReferenceResolveSync);
            
            projects.Projects.View(lifetime, (projectLifetime, project) =>
            {
                myProjectLifetimes.Add(project, projectLifetime);

                if (!project.IsUnityProject())
                    return;

                Handle(project);
            });
        }

        private void Handle(IProject project)
        {
            Lifetime projectLifetime;
            if (!myProjectLifetimes.TryGetValue(project, out projectLifetime))
                return;

            var exceptions = new LocalList<Exception>();
            foreach (var handler in myHandlers)
            {
                try
                {
                    handler(projectLifetime, project);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Failed to handle project changes", exceptions.ToArray());
            }
        }

        public void RegisterProjectChangeHandler(Action<Lifetime, IProject> handler)
        {
            myHandlers.Add(handler);
        }

        object IChangeProvider.Execute(IChangeMap changeMap)
        {
            var projectModelChange = changeMap.GetChange<ProjectModelChange>(mySolution);
            if (projectModelChange == null)
                return null;

            // ReSharper hasn't necessarily processed all references when it adds the IProject
            // to the IViewableProjectsCollection. Keep an eye on reference changes, add the
            // project settings if/when the project becomes a unity project
            var projects = new JetHashSet<IProject>();
            var changes = ReferencedAssembliesService.TryGetAssemblyReferenceChanges(projectModelChange, ProjectExtensions.UnityReferenceNames);
            foreach (var change in changes)
                projects.Add(change.GetNewProject());

            foreach (var project in projects)
            {
                myChangeManager.ExecuteAfterChange(() =>
                {
                    if (project.IsUnityProject())
                        Handle(project);
                });
            }

            return null;
        }
    }
}