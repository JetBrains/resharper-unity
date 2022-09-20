namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches
{
    public class InputActionItem
    {
        private readonly string myName;
        private readonly int myNameOffset;

        public InputActionItem(string name, int nameOffset)
        {
            myName = name;
            myNameOffset = nameOffset;
        }
        
        public override string ToString()
        {
            return $"{myName}:{myNameOffset}";
        }
    }
}