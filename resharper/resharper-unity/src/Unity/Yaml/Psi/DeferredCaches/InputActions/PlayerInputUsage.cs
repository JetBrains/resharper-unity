using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions
{
    public class PlayerInputUsage
    {
        public PlayerInputUsage(LocalReference location,
                              Guid inputActionsFileGuid)
        {
            Location = location;
            InputActionsFileGuid = inputActionsFileGuid;
        }

        public LocalReference Location { get; } 
        public Guid InputActionsFileGuid { get; }

        [CanBeNull]
        public static PlayerInputUsage ReadFrom([NotNull] UnsafeReader reader)
        {
            var reference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var guid = reader.ReadGuid();
            return new PlayerInputUsage(reference, guid);
        }

        public void WriteTo([NotNull] UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            writer.Write(InputActionsFileGuid);
        }
    }
}