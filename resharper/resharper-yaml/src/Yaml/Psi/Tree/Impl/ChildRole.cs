namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl
{
  public static class ChildRole
  {
    public const short NONE = 0;

    public const short YAML_TEXT = 1;
    public const short YAML_PROPERTIES = 2;
    public const short YAML_INDICATOR = 3;
    public const short YAML_INDENT = 4;

    public const short LAST = 2;
  }
}