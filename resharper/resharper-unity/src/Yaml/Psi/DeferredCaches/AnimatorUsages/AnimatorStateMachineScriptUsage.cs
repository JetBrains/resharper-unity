using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    public class AnimatorStateMachineScriptUsage : IAnimatorScriptUsage
    {
        public string Name { get; }
        public LocalList<long> ScriptsAnchors { get; }
        public LocalList<long> ChildStateMachinesAnchors { get; }
        public LocalList<long> ChildStatesAnchors { get; }
        public LocalReference Location { get; }

        public AnimatorStateMachineScriptUsage(LocalReference location, 
                                               [NotNull] string name,
                                               LocalList<long> scriptsAnchors,
                                               LocalList<long> childStateMachinesAnchors,
                                               LocalList<long> childStatesAnchors)
        {
            Location = location;
            Name = name;
            ScriptsAnchors = scriptsAnchors;
            ChildStateMachinesAnchors = childStateMachinesAnchors;
            ChildStatesAnchors = childStatesAnchors;
        }

        public override bool Equals(object obj)
        {
            return obj is AnimatorStateMachineScriptUsage other &&
                   Location.Equals(other.Location) && 
                   Name.Equals(other.Name);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                return (Location.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
        
        public void WriteTo(UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            writer.Write(Name);
            WriteAnchors(writer, ScriptsAnchors);
            WriteAnchors(writer, ChildStateMachinesAnchors);
            WriteAnchors(writer, ChildStatesAnchors);
        }

        private static void WriteAnchors([NotNull] UnsafeWriter writer, LocalList<long> anchors)
        {
            writer.Write(anchors.Count);
            foreach (var anchor in anchors) writer.Write(anchor);
        }

        [NotNull]
        public static AnimatorStateMachineScriptUsage ReadFrom([NotNull] UnsafeReader reader)
        {
            var animatorStateReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var animatorStateName = reader.ReadString();
            var stateMachineBehavioursAnchors = ReadAnchors(reader);
            var childStateMachinesAnchors = ReadAnchors(reader);
            var childStatesAnchors = ReadAnchors(reader);
            return new AnimatorStateMachineScriptUsage(animatorStateReference, animatorStateName ?? "", 
                stateMachineBehavioursAnchors, childStateMachinesAnchors, childStatesAnchors);
        }

        private static LocalList<long> ReadAnchors([NotNull] UnsafeReader reader)
        {
            var stateMachineBehavioursAnchorsCount = reader.ReadInt();
            var stateMachineBehavioursAnchors = new LocalList<long>(stateMachineBehavioursAnchorsCount);
            for (var i = 0; i < stateMachineBehavioursAnchorsCount; i++)
                stateMachineBehavioursAnchors.Add(reader.ReadLong());
            return stateMachineBehavioursAnchors;
        }

    }
}