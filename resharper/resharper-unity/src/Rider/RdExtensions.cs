using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
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
    }
}