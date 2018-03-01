using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class Extensions
    {
        public static void SetCustomData(this Solution solution, string key, string value)
        {
            var data = solution.CustomData.Data;
            if (data.ContainsKey(key))
                data[key] = value;
            else
                data.Add(key, value);
        }
    }
}