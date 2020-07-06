using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.ProjectModel.SharedProjects;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.ProjectModel
{
    [SolutionComponent]
    public class UnityPlayerProjectOperations : ISharedProjectOperations
    {
        private const string PlayerProjectSuffix = ".Player";
        private readonly ISolution mySolution;

        public UnityPlayerProjectOperations(ISolution solution)
        {
            mySolution = solution;
        }

        public IList<IProjectItem> GetProjectItemInSharedProjects(IProjectItem projectItem)
        {
            var playerProject = projectItem.GetProject().NotNull();
            if (!playerProject.Name.EndsWith(PlayerProjectSuffix))
                return EmptyList<IProjectItem>.InstanceList;

            var originalProject = mySolution
                .GetProjectsByName(playerProject.Name.RemoveEnd(PlayerProjectSuffix))
                .SingleItem();
            if (originalProject == null)
                return EmptyList<IProjectItem>.InstanceList;

            return originalProject.FindProjectItemsByLocation(projectItem.Location).ToList();
        }

        public IList<IProjectFolder> GetOrCreateParentSharedFoldersInReferencedProjects(IProjectFolder projectFolder)
        {
            // TODO: create new parent project folder if needed
            return GetSharedProjectItemsInReferencedProjects(projectFolder).OfType<IProjectFolder>().ToList();
        }

        public IList<IProjectItem> GetSharedProjectItemsInReferencedProjects(IProjectItem projectItem)
        {
            var project = projectItem.GetProject().NotNull();
            var playerProject = mySolution
                .GetProjectsByName(project.Name + PlayerProjectSuffix)
                .SingleItem();
            if (playerProject == null)
                return EmptyList<IProjectItem>.InstanceList;

            return playerProject.FindProjectItemsByLocation(projectItem.Location).ToList();
        }
    }
}