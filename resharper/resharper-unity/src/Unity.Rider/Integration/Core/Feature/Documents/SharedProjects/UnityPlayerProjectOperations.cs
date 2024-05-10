using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers.Transactions.ProjectHostActions.SharedProjects;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Documents.SharedProjects
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
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
            if (!playerProject.Name.EndsWith(PlayerProjectSuffix)) // todo: check that define `UNITY_EDITOR` is not be present
                return EmptyList<IProjectItem>.InstanceList;

            var originalProject = mySolution
                .GetProjectsByName(playerProject.Name.RemoveEnd(PlayerProjectSuffix))
                .SingleItem();
            if (originalProject == null)
                return EmptyList<IProjectItem>.InstanceList;

            return originalProject.FindProjectItemsByLocation(projectItem.Location).ToList();
        }

        public IList<IProjectFolder> GetOrCreateParentSharedFoldersInReferencedProjects(IProjectFolder projectFolder,
            VirtualFileSystemPath location, bool isFolder)
        {
            // RIDER-97069 Add new non-cs file for .Player projects in Unity
            if (location.ExtensionNoDot == "cs" || isFolder)
            {
                return GetSharedProjectItemsInReferencedProjects(projectFolder).OfType<IProjectFolder>().ToList();    
            }

            return EmptyList<IProjectFolder>.InstanceList;
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