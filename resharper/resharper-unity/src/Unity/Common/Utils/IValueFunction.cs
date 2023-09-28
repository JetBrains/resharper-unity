#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils
{
    public interface IValueFunction<in T, out TResult>
    {
        TResult Invoke(T arg);
    }
}