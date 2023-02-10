using System;
using JetBrains.Annotations;
namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
    [Serializable]
    internal class AnimationElement : AbstractUsageElement
    {
        public AnimationElement(string filePath, string fileName, [NotNull] string[] path, int[] rootIndices)
            : base(filePath, fileName, path, rootIndices)
        {
        }

        public override string StartNodeImage => "AnimationClip Icon";
        public override string NodeImage => "AnimationClip Icon";
        public override string TerminalNodeImage => NodeImage;
    }
}