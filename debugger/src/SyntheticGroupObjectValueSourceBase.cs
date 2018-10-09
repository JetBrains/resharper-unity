using System;
using JetBrains.Annotations;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public abstract class SyntheticGroupObjectValueSourceBase : RemoteFrameObject, IObjectValueSource<SoftEvaluationContext>
    {
        private readonly ILogger myLogger;
        private readonly ExpressionEvaluator<SoftEvaluationContext, TypeMirror, Value> myExpressionEvaluator;

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
        }

        public SoftEvaluationContext Context { get; }
        public IDebuggerHierarchicalObject ParentSource { get; }
        public string Name { get; }

        protected SoftDebuggerAdaptor Adaptor { get; }

        public ObjectValue[] GetChildren(ObjectPath path, int index, int count, IEvaluationOptions options)
        {
            try
            {
                return GetChildrenSafe(path, index, count, options);
            }
            catch (Exception e)
            {
                myLogger.Error(e);
            }

            return EmptyArray<ObjectValue>.Instance;
        }

        protected abstract ObjectValue[] GetChildrenSafe(ObjectPath path, int index, int count,
                                                         IEvaluationOptions options);

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

        [CanBeNull]
        protected TypeMirror GetType(string typename)
        {
            return Adaptor.GetType(Context, typename);
        }

        [CanBeNull]
        protected Value InvokeInstanceMethod([NotNull] Value target, string methodName, params Value[] parameters)
        {
            return Adaptor.Invocator.InvokeInstanceMethod(Context, target, methodName, parameters)?.Result;
        }

        [CanBeNull]
        protected Value InvokeStaticMethod([NotNull] TypeMirror type, string methodName, params Value[] parameters)
        {
            return Adaptor.Invocator.InvokeStaticMethod(Context, type, methodName, parameters)?.Result;
        }
    }
}
