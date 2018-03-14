using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class Extensions
    {
        public static void SetModelData(this RiderUnityHost host, string key, string value)
        {
            var data = host.Model.Data;
            if (data.ContainsKey(key))
                data[key] = value;
            else
                data.Add(key, value);
        }
    }
}