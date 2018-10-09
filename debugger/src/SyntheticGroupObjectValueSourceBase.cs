using System;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.RuntimeInvocation;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public abstract class SyntheticGroupObjectValueSourceBase : RemoteFrameObject, IObjectValueSource<SoftEvaluationContext>
    {
        private readonly ILogger myLogger;
        private readonly ExpressionEvaluator<SoftEvaluationContext, TypeMirror, Value> myExpressionEvaluator;
        private readonly SoftRuntimeInvocator mySoftRuntimeInvocator;

        protected SyntheticGroupObjectValueSourceBase(SoftEvaluationContext context,
                                                      IDebuggerHierarchicalObject parentSource, string name,
                                                      ILogger logger)
        {
            myLogger = logger;
            Context = context;
            ParentSource = parentSource;
            Name = name;

            Adaptor = context.Session.Adapter;
            myExpressionEvaluator = context.Session.DefaultEvaluator;
            mySoftRuntimeInvocator = Adaptor.Invocator;
        }

        public SoftEvaluationContext Context { get; }
        public IDebuggerHierarchicalObject ParentSource { get; }
        public string Name { get; }

        protected SoftDebuggerAdaptor Adaptor { get; }

        public abstract ObjectValue[] GetChildren(ObjectPath path, int index, int count, IEvaluationOptions options);

        public ValuePresentation SetValue(ObjectPath path, string value, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        public ObjectValue GetValue(ObjectPath path, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        public IRawValue GetRawValue(ObjectPath path, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        public void SetRawValue(ObjectPath path, IRawValue value, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        protected ValueReference<SoftEvaluationContext, TypeMirror, Value> Evaluate(string expression)
        {
            return myExpressionEvaluator.Evaluate(Context, expression);
        }

        protected TypeMirror GetType(string typename)
        {
            return Adaptor.GetType(Context, typename);
        }

        protected Value Invoke(MethodMirror method, TypeMirror type, Value instance, params Value[] parameters)
        {
            return mySoftRuntimeInvocator.RuntimeInvoke(Context, method, type, instance, parameters).Result;
        }
    }
}
