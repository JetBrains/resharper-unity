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

        public override bool PropagateIfUnmarked(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
            if (rootMarks.Contains(vertex))
                return true;

            if (banMarks.Contains(vertex) == false)
                return base.PropagateIfUnmarked(structure, rootMarks, banMarks, vertex);

            foreach (var (parent, _) in structure.InvertedEdges.GetOrEmpty(vertex))
            {
                if (banMarks.Contains(parent))
                    continue;

                if (base.PropagateIfUnmarked(structure, rootMarks, banMarks, parent))
                    return true;
            }

            return false;
        }
    }
}