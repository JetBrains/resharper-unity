using System.Collections.Generic;
using JetBrains.Collections.Viewable;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.HotReload;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    public abstract class DelegatingDebuggerSessionOptions : IDebuggerSessionOptions
    {
        protected readonly IDebuggerSessionOptions DebuggerSessionOptionsImplementation;

        protected DelegatingDebuggerSessionOptions(IDebuggerSessionOptions debuggerSessionOptionsImplementation)
        {
            DebuggerSessionOptionsImplementation = debuggerSessionOptionsImplementation;
        }

        public IEvaluationOptions EvaluationOptions => DebuggerSessionOptionsImplementation.EvaluationOptions;
        public bool StepOverPropertiesAndOperators => DebuggerSessionOptionsImplementation.StepOverPropertiesAndOperators;
        public bool ProjectAssembliesOnly => DebuggerSessionOptionsImplementation.ProjectAssembliesOnly;
        public IViewableProperty<bool> ProcessExceptionsOutsideMyCode => DebuggerSessionOptionsImplementation.ProcessExceptionsOutsideMyCode;
        public bool BreakOnUserUnhandledExceptions => DebuggerSessionOptionsImplementation.BreakOnUserUnhandledExceptions;
        public IReadOnlyList<string> UserUnhandledExceptionsIgnoreList => DebuggerSessionOptionsImplementation.UserUnhandledExceptionsIgnoreList;
        public virtual bool BreakOnUnhandledExceptions => DebuggerSessionOptionsImplementation.BreakOnUnhandledExceptions;
        public bool DisableJitOptimizationOnModuleLoad => DebuggerSessionOptionsImplementation.DisableJitOptimizationOnModuleLoad;
        public bool EnableExternalSourceDebug => DebuggerSessionOptionsImplementation.EnableExternalSourceDebug;
        public bool StepIntoExternalCodeSupported => DebuggerSessionOptionsImplementation.StepIntoExternalCodeSupported;
        public bool AutomaticallyRefreshWatches => DebuggerSessionOptionsImplementation.AutomaticallyRefreshWatches;
        public bool EditAndContinueEnabled => DebuggerSessionOptionsImplementation.EditAndContinueEnabled;
        public bool EditAndContinueSupported => DebuggerSessionOptionsImplementation.EditAndContinueSupported;
        public bool ShowReturnValues => DebuggerSessionOptionsImplementation.ShowReturnValues;
        public int ReturnValuesTimeout => DebuggerSessionOptionsImplementation.ReturnValuesTimeout;
        public DebugKind DebugKind => DebuggerSessionOptionsImplementation.DebugKind;
        public bool IsRemoteDebug => DebuggerSessionOptionsImplementation.IsRemoteDebug;
        public bool PathMapResolverEnabled => DebuggerSessionOptionsImplementation.PathMapResolverEnabled;
        public bool BackendUrlResolverEnabled => DebuggerSessionOptionsImplementation.BackendUrlResolverEnabled;
        public bool EnableHeuristicPathResolve => DebuggerSessionOptionsImplementation.EnableHeuristicPathResolve;
        public bool DisableDebugHeap => DebuggerSessionOptionsImplementation.DisableDebugHeap;
        public bool IgnorePrecompiledAssemblies => DebuggerSessionOptionsImplementation.IgnorePrecompiledAssemblies;
        public bool ForceLoadMethodByToken => DebuggerSessionOptionsImplementation.ForceLoadMethodByToken;
        public HotReloadInfo HotReload => DebuggerSessionOptionsImplementation.HotReload;
        public bool DisableRuntimeLoadTimeout => DebuggerSessionOptionsImplementation.DisableRuntimeLoadTimeout;
        public bool DisableSteppingHandlers => DebuggerSessionOptionsImplementation.DisableSteppingHandlers;
        public bool TrackHandledExceptionsInAsyncCode => DebuggerSessionOptionsImplementation.TrackHandledExceptionsInAsyncCode;
    }
}