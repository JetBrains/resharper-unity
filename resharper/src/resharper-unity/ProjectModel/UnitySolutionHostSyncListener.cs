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
            IsAbleToEstablishProtocolConnectionWithUnity = new Lazy<bool>(ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory));
        }

        public Lazy<bool> IsAbleToEstablishProtocolConnectionWithUnity;


        public override void BeforeChangeProjects(ICollection<IProjectMark> projectMarks)
        {
            base.BeforeChangeProjects(projectMarks);
            IsAbleToEstablishProtocolConnectionWithUnity = new Lazy<bool>(ProjectExtensions.IsAbleToEstablishProtocolConnectionWithUnity(mySolution.SolutionDirectory));
        }
    }
}