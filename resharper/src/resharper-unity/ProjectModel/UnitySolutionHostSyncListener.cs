using System;
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

        private Lazy<bool> isAbleToEstablishProtocolConnectionWithUnityCached;
        public bool IsAbleToEstablishProtocolConnectionWithUnity
        {
            get
            {
                if (isAbleToEstablishProtocolConnectionWithUnityCached.IsValueCreated)
                    return isAbleToEstablishProtocolConnectionWithUnityCached.Value;
                isAbleToEstablishProtocolConnectionWithUnityCached = new Lazy<bool>(ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory));
                return isAbleToEstablishProtocolConnectionWithUnityCached.Value;
            }
        }

        public override void BeforeChangeProjects(ICollection<IProjectMark> projectMarks)
        {
            base.BeforeChangeProjects(projectMarks);
            isAbleToEstablishProtocolConnectionWithUnityCached = new Lazy<bool>(ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory));
        }
    }
}