using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    public class AnimatorStateScriptUsage : IAnimatorScriptUsage
    {
        public AnimatorStateScriptUsage(LocalReference location, 
                                        [NotNull] string name, 
                                        LocalList<long> scriptsAnchors,
                                        Guid? animReference)
        {
            Location = location;
            Name = name;
            ScriptsAnchors = scriptsAnchors;
            AnimReference = animReference;
        }
        
        public string Name { get; }
        public LocalList<long> ScriptsAnchors { get; }
        public LocalReference Location { get; }
        
        public Guid? AnimReference { get; }

        public void WriteTo(UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            writer.Write(Name);
            WriteStateMachineBehavioursAnchors(writer);
            if (writer.WriteNullness(AnimReference)) writer.Write(AnimReference.Value);
        }

        private void WriteStateMachineBehavioursAnchors([NotNull] UnsafeWriter writer)
        {
            var stateMachineBehavioursAnchors = ScriptsAnchors;
            writer.Write(stateMachineBehavioursAnchors.Count);
            foreach (var anchor in stateMachineBehavioursAnchors) writer.Write(anchor);
        }

        public static AnimatorStateScriptUsage ReadFrom([NotNull] UnsafeReader reader)
        {
            var animatorStateReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var animatorStateName = reader.ReadString();
            var stateMachineBehavioursAnchors = ReadStateMachineBehavioursAnchors(reader);
            var animExternalReference = reader.ReadNullness() ? reader.ReadGuid().ToNullable() : null;
            return new AnimatorStateScriptUsage(animatorStateReference, animatorStateName ?? "",
                stateMachineBehavioursAnchors, animExternalReference);
        }

        private static LocalList<long> ReadStateMachineBehavioursAnchors([NotNull] UnsafeReader reader)
        {
            var stateMachineBehavioursAnchorsCount = reader.ReadInt();
            var stateMachineBehavioursAnchors = new LocalList<long>(stateMachineBehavioursAnchorsCount);
            for (var i = 0; i < stateMachineBehavioursAnchorsCount; i++) 
                stateMachineBehavioursAnchors.Add(reader.ReadLong());
            return stateMachineBehavioursAnchors;
        }
        
        public override bool Equals(object obj)
        {
            return obj is AnimatorStateScriptUsage other && Location.Equals(other.Location) && Name.Equals(other.Name);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                return (Location.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
    }
}