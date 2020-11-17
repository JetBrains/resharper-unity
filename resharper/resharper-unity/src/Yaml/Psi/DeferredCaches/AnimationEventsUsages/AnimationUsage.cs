using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages
{
    public class AnimationUsage
    {
        public AnimationUsage(LocalReference location,
                              [NotNull] string animationName,
                              int sampleRate,
                              [NotNull] string functionName,
                              double time,
                              Guid guid)
        {
            Location = location;
            AnimationName = animationName;
            SampleRate = sampleRate;
            FunctionName = functionName;
            Time = time;
            Guid = guid;
        }

        public LocalReference Location { get; }

        [NotNull]
        public string AnimationName { get; }

        [NotNull]
        public string FunctionName { get; }

        public int SampleRate { get; }

        public double Time { get; }

        public Guid Guid { get; }

        [CanBeNull]
        public static AnimationUsage ReadFrom([NotNull] UnsafeReader reader)
        {
            var animationReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var animationName = reader.ReadString();
            var sampleRate = reader.ReadInt();
            var functionName = reader.ReadString();
            if (animationName is null || functionName is null) return null;
            var time = reader.ReadDouble();
            var guid = reader.ReadGuid();
            return new AnimationUsage(animationReference, animationName, sampleRate, functionName, time, guid);
        }

        public void WriteTo([NotNull] UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            writer.Write(AnimationName);
            writer.Write(SampleRate);
            writer.Write(FunctionName);
            writer.Write(Time);
            writer.Write(Guid);
        }
    }
}