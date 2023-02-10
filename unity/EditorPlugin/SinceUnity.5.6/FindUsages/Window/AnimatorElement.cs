using System;
using JetBrains.Annotations;
using JetBrains.Rider.Model.Unity.BackendUnity;

namespace JetBrains.Rider.Unity.Editor.FindUsages.Window
{
    [Serializable]
    internal class AnimatorElement : AbstractUsageElement
    {
        [NotNull]
        public const string AnimatorStateMachineIcon = "AnimatorStateMachine Icon";

        [NotNull]
        public const string AnimatorStateIcon = "AnimatorState Icon";

        [NotNull] public string[] PathElements { get; }
        public AnimatorUsageType Type { get; }


        public AnimatorElement(AnimatorUsageType type,
                               string filePath,
                               string fileName,
                               [NotNull] string[] path,
                               int[] rootIndices)
            : base(filePath, fileName, path, rootIndices)
        {
            PathElements = path;
            Type = type;
        }

        public override string NodeImage => Type == AnimatorUsageType.StateMachine ? AnimatorStateMachineIcon : AnimatorStateIcon;
        public override string StartNodeImage => NodeImage;
        public override string TerminalNodeImage => NodeImage;
    }
}