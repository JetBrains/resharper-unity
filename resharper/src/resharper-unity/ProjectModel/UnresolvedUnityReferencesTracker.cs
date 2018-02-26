using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.changes;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionComponent]
    public class UnresolvedUnityReferencesTracker : IChangeProvider
    {
        public interface IHandler
        {
            void OnReferenceAdded(IProject unityProject);
        }
        
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        
        private readonly List<IHandler> myHandlers;
        private readonly ISolution mySolution;
        
        private readonly ModuleReferenceResolveSync myModuleReferenceResolveSync;
        private readonly ChangeManager myChangeManager;

        public UnresolvedUnityReferencesTracker(
            Lifetime lifetime,
            ILogger logger,
            
            ISolution solution,
            IEnumerable<IHandler> handlers,
            
            ModuleReferenceResolveSync moduleReferenceResolveSync,
            ChangeManager changeManager)
        {
            myLifetime = lifetime;
            myLogger = logger;
            
            mySolution = solution;
            myHandlers = handlers.ToList();
            
            myModuleReferenceResolveSync = moduleReferenceResolveSync;
            myChangeManager = changeManager;
            
            myChangeManager.RegisterChangeProvider(myLifetime, this);
            myChangeManager.AddDependency(myLifetime, this, myModuleReferenceResolveSync);
        }

        private void Handle(IProject project)
        {
            var exceptions = new LocalList<Exception>();
            foreach (var handler in myHandlers)
            {
                try
                {
                    handler.OnReferenceAdded(project);
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
        
        public object Execute(IChangeMap changeMap)
        {
            var change = changeMap.GetChange<ProjectModelChange>(mySolution);
            if (change == null) return null;

            var projects = new JetHashSet<IProject>();
            
            var unresolved = GetUnresolvedReferences(change);
            foreach (var reference in unresolved)
            {
                {
                    myLogger.Warn($"Unresolved Unity reference: {reference.ReferenceTarget.AssemblyName}");
                    projects.Add(reference.GetProject());
                }
            }

            foreach (var project in projects)
            {
                myChangeManager.ExecuteAfterChange(() =>
                {
                    Handle(project);
                });
            }

            return null;
        }

        private LocalList<IProjectToUnresolvedAssemblyReference> GetUnresolvedReferences(ProjectModelChange projectModelChange)
        {
            var result = new LocalList<IProjectToUnresolvedAssemblyReference>();
            var q = new Queue<ProjectModelChange>();
            q.Enqueue(projectModelChange);

            while (q.Count > 0)
            {
                var change = q.Dequeue();
                if (change == null)
                    continue;
                
                var reference = change.ProjectModelElement as IProjectToUnresolvedAssemblyReference;
                if (reference != null &&
                    ProjectExtensions.UnityReferenceNames.Contains(reference.ReferenceTarget.AssemblyName) &&
                    change.IsAdded)
                {
                    result.Add(reference);
                }

                foreach (var c in change.GetChildren())
                {
                    q.Enqueue(c);
                }
            }
            
            return result;
        }
    }
}