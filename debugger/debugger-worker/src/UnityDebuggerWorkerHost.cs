using Autofac;
using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Autofac;

namespace JetBrains.Debugger.Worker.Plugins.Unity
{
    [DebuggerGlobalComponent]
    public class UnityDebuggerWorkerHost : IStartable
    {
        public UnityDebuggerWorkerHost(RiderDebuggerWorker debuggerWorker)
        {
            // Get/create the model. This registers serialisers for all types in the model, including the start info
            // derived types used in the root protocol, not in our extension
            Model = debuggerWorker.FrontendModel.GetUnityDebuggerWorkerModel();
        }

        public UnityDebuggerWorkerModel Model { get; }

        void IStartable.Start()
        {
            // Do nothing. IStartable means Autofac will eagerly create the component but we do all our work in the ctor
        }
    }
}