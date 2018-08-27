using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionInstanceComponent]
    public class UnitySolutionHostSyncListener:SolutionHostSyncListener
    {
        private readonly ISolution mySolution;

        public UnitySolutionHostSyncListener(ISolution solution)
        {
            mySolution = solution;
        }

        private bool? isAbleToEstablishProtocolConnectionWithUnityCached;
        public bool IsAbleToEstablishProtocolConnectionWithUnity
        {
            get
            {
                if (isAbleToEstablishProtocolConnectionWithUnityCached != null)
                    return isAbleToEstablishProtocolConnectionWithUnityCached.Value;
                isAbleToEstablishProtocolConnectionWithUnityCached =
                    ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory);
                return isAbleToEstablishProtocolConnectionWithUnityCached.Value;
            }
        }

        public override void BeforeChangeProjects(ICollection<IProjectMark> projectMarks)
        {
            base.BeforeChangeProjects(projectMarks);
            isAbleToEstablishProtocolConnectionWithUnityCached = ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory);
        }
    }
}