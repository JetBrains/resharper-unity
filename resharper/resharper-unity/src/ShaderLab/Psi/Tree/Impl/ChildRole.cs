namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree.Impl
{
    public static class ChildRole
    {
        public const short NONE = 0;
        public const short SHADER_LAB_COMMAND = 1;
        public const short SHADER_LAB_KEYWORD = 2;
        public const short SHADER_LAB_VALUE = 3;
        public const short SHADER_LAB_CONSTANT = 4;
        public const short SHADER_LAB_IDENTIFIER = 5;
        public const short SHADER_LAB_NAME = 6;
        public const short SHADER_LAB_REFERENCE = 7;
        public const short LAST = 100;
    }
}