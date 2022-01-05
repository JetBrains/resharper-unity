using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    public class SimplePropagator : ICallGraphPropagator
    {
        public void RemoveEdge(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId startVertex,
            ElementId endVertex)
        {
        }

        public void UnrootVertex(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
        }

        public void PermitVertex(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
        }

        public void RemoveVertex(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
        }

        public void AddVertex(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
        }

        public void BanVertex(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
        }

        public void RootVertex(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
        }

        public void AddEdge(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId startVertex,
            ElementId endVertex)
        {
        }

        public bool IsMarkedLocal(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
            return false;
        }

        public bool IsMarkedGlobal(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
            return false;
        }

        public bool PropagateIfUnmarked(CallGraphStructure structure, IReadonlyCountingSet<ElementId> rootMarks, IReadonlyCountingSet<ElementId> banMarks, ElementId vertex)
        {
            return false;
        }

        public void Clear()
        {
        }
    }
}