#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils
{
    public interface IValueFunction<in T, out TResult>
    {
        TResult Invoke(T arg);
    }
    
    public interface IValueFunction<in T1, in T2, out TResult>
    {
        TResult Invoke(T1 arg1, T2 arg2);
    }
}