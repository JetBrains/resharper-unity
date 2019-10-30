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
    public abstract class SyntheticGroupObjectValueSourceBase : IObjectValueSource<SoftEvaluationContext>
    {
        private readonly ILogger myLogger;
        private readonly IExpressionEvaluators<SoftEvaluationContext, TypeMirror, Value> myExpressionEvaluator;

        protected SyntheticGroupObjectValueSourceBase(SoftEvaluationContext context,
                                                      IDebuggerHierarchicalObject parentSource, string name,
                                                      ILogger logger)
        {
            myLogger = logger;
            Context = context;
            ParentSource = parentSource;
            Name = name;

            Adaptor = context.Session.Adapter;
            myExpressionEvaluator = context.Session.Evaluators;
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

        IEvaluationContext IObjectValueSource.Context => Context;

        protected ValueReference<SoftEvaluationContext, TypeMirror, Value> Evaluate(string expression)
        {
            return myExpressionEvaluator.Evaluate(Context, expression);
        }

        [CanBeNull]
        protected TypeMirror GetType(string typename)
        {
            var typeMirror = Adaptor.GetType(Context, typename);
            if (typeMirror == null)
            {
                myLogger.Warn("Unable to get type {0}", typename);
                return null;
            }

            return typeMirror;
        }

        [CanBeNull]
        protected Value InvokeInstanceMethod([NotNull] Value target, string methodName, params Value[] parameters)
        {
            var result = Adaptor.Invocator.InvokeInstanceMethod(Context, target, methodName, parameters);
            if (result == null)
            {
                var args = new string[2 + parameters.Length];
                args[0] = target.ToString();
                args[1] = methodName;
                Array.Copy(parameters, 0, args, 2, parameters.Length);
                // ReSharper disable FormatStringProblem
                myLogger.Warn("InvokeStaticMethod returned null for {0}.{1}({2})", args);
                // ReSharper restore FormatStringProblem
                return null;
            }

            return result.Result;
        }

        [CanBeNull]
        protected Value InvokeStaticMethod([NotNull] TypeMirror type, string methodName, params Value[] parameters)
        {
            var result = Adaptor.Invocator.InvokeStaticMethod(Context, type, methodName, parameters);
            if (result == null)
            {
                var args = new string[2 + parameters.Length];
                args[0] = type.FullName;
                args[1] = methodName;
                Array.Copy(parameters, 0, args, 2, parameters.Length);
                // ReSharper disable FormatStringProblem
                myLogger.Warn("InvokeStaticMethod returned null for {0}.{1}({2})", args);
                // ReSharper restore FormatStringProblem
                return null;
            }

            return result.Result;
        }

        [CanBeNull]
        protected Value GetMember([NotNull] Value value, string memberName)
        {
            var member = Adaptor.GetMember(Context, null, value, memberName);
            if (member == null)
            {
                myLogger.Warn("Unable to get member {0} from {1}", memberName, value.Type.FullName);
                return null;
            }

            return member.Value;
        }

        protected ObjectValue CreateObjectValue(string name, Value value, IEvaluationOptions options)
        {
            return LiteralValueReference.CreateTargetObjectLiteral(Adaptor, Context, name, value)
                .CreateObjectValue(options);
        }
    }
}
