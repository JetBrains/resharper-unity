namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    public static class Extensions
    {
        public static void SetModelData(this UnityHost host, string key, string value)
        {
            host.PerformModelAction(m =>
            {
                var data = m.Data;
                if (data.ContainsKey(key))
                    data[key] = value;
                else
                    data.Add(key, value);
            });
        }
    }
}