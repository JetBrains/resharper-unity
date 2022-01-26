using System;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol
{
    public static class RdExtensions
    {
        public static RdTask<T> ToRdTask<T>(this IRdTask<T> task, Lifetime lifetime)
        {
            if (task is RdTask<T> rdTask)
                return rdTask;

            var newRdTask = new RdTask<T>();
            task.Result.Advise(lifetime, result =>
            {
                switch (result.Status)
                {
                    case RdTaskStatus.Success:
                        newRdTask.Set(result.Result);
                        break;
                    case RdTaskStatus.Canceled:
                        newRdTask.SetCancelled();
                        break;
                    case RdTaskStatus.Faulted:
                        newRdTask.Set(result.Error);
                        break;
                }
            });
            return newRdTask;
        }

        // BeUtilExtensions.FlowIntoRd subscribes to Advise on source.Change, which is a signal. This means it doesn't
        // send the acknowledgement (i.e. set the current value immediately) as part of Advise. FlowIntoRd will set the
        // target value from the source itself, but doesn't check if the source value has been set, which can cause
        // exceptions. We'll Advise the source itself, which does check the source value, and sends an acknowledgement
        public static void FlowIntoRdSafe<TValue>(
            [NotNull] this IViewableProperty<TValue> source,
            Lifetime lifetime,
            [NotNull] IViewableProperty<TValue> target)
        {
            source.Advise(lifetime, val => target.Value = val);
        }

        public static void FlowIntoRdSafe<TValue, TResult>(
            [NotNull] this IViewableProperty<TValue> source,
            Lifetime lifetime,
            Func<TValue, TResult> converter,
            [NotNull] IViewableProperty<TResult> target)
        {
            source.Advise(lifetime, val => target.Value = converter(val));
        }

        public static void FlowChangesIntoRdDeferred<TValue>(
            [NotNull] this IViewableProperty<TValue> source,
            Lifetime lifetime,
            [NotNull] Func<IViewableProperty<TValue>> nullableTargetCreator)
        {
            // If the source has a value, Advise will immediately try to set it on target
            source.Advise(lifetime, val =>
            {
                var target = nullableTargetCreator();
                if (target != null) target.Value = val;
            });
        }
    }
}