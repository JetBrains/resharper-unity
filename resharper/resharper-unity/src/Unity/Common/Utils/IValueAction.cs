#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils
{
    public interface IValueAction<in T>
    {
        void Invoke(T arg);
    }
}