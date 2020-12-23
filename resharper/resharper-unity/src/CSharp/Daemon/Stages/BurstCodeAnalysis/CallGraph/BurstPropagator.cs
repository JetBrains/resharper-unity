using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    public class BurstPropagator : CallGraphOutcomingPropagator
    {
        public BurstPropagator(ISolution solution, string markName)
            : base(solution, markName)
        {
        }

        public override bool IsMarkedLocal(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks,
            IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
            var strict = structure.BanMarks.GetOrEmpty(BurstStrictlyBannedMarkProvider.RootMarkId);
            return rootMarks.Contains(vertex) && !strict.Contains(vertex);
        }

        public override bool IsMarkedGlobal(
            CallGraphStructure structure,
            IReadonlyCountingSet<ElementId> rootMarks,
            IReadonlyCountingSet<ElementId> banMarks,
            ElementId vertex)
        {
            var strict = structure.BanMarks.GetOrEmpty(BurstStrictlyBannedMarkProvider.RootMarkId);

            // method has burst discard and resharper.disable
            if (strict.Contains(vertex))
                return false;

            if (rootMarks.Contains(vertex))
                return true;

            // method has no managed code
            if (banMarks.Contains(vertex) == false)
                return base.IsMarkedGlobal(structure, rootMarks, banMarks, vertex);

            // method has managed code, question - does it have direct burst route?
            foreach (var (parent, _) in structure.InvertedEdges.GetOrEmpty(vertex))
            {
                if (banMarks.Contains(parent))
                    continue;

                if (base.IsMarkedGlobal(structure, rootMarks, banMarks, parent))
                    return true;
            }

            return false;
        }
    }
}