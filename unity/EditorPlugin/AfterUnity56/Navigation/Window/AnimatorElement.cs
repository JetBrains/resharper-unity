using System;
using JetBrains.Annotations;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
    [Serializable]
    internal class AnimatorElement : AbstractUsageElement
    {
        [NotNull] public string[] PathElements { get; }
        
        public AnimatorElement(string filePath, string fileName, [NotNull] string[] path, int[] rootIndices)
            : base(filePath, fileName, path, rootIndices)
        {
            PathElements = path;
        }

        // TODO: Change
        public override string StartNodeImage => "ScriptableObject Icon";
        public override string NodeImage => "ScriptableObject Icon";
        public override string TerminalNodeImage => NodeImage;
    }
}