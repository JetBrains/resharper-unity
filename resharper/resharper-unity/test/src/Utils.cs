namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
  public static class Utils
  {
    public static readonly string ProductGoldSuffix =
#if RIDER
        "rider"
#else
        "resharper"
#endif
      ;
  }
}