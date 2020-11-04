using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    [PolymorphicMarshaller]
    public class AnimatorUsagesDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as AnimatorUsagesDataElement);

        [NotNull] public readonly IDictionary<long, long> ChildToParent;
        [NotNull] public readonly OneToListMap<Guid, long> GuidToAnchors;
        [NotNull] public readonly OneToListMap<long, AnimatorStateMachineScriptUsage> ScriptAnchorToStateMachineUsages;
        [NotNull] public readonly OneToListMap<long, AnimatorStateScriptUsage> ScriptAnchorToStateUsages;
        [NotNull] public readonly IDictionary<long, AnimatorStateMachineScriptUsage> StateMachineAnchorToUsage;

        public AnimatorUsagesDataElement()
        {
            GuidToAnchors = new OneToListMap<Guid, long>();
            ScriptAnchorToStateUsages = new OneToListMap<long, AnimatorStateScriptUsage>();
            ScriptAnchorToStateMachineUsages = new OneToListMap<long, AnimatorStateMachineScriptUsage>();
            StateMachineAnchorToUsage = new Dictionary<long, AnimatorStateMachineScriptUsage>();
            ChildToParent = new Dictionary<long, long>();
        }

        private AnimatorUsagesDataElement([NotNull] OneToListMap<Guid, long> guidToAnchors,
                                          [NotNull]
                                          OneToListMap<long, AnimatorStateScriptUsage> scriptAnchorToStateUsages,
                                          [NotNull] OneToListMap<long, AnimatorStateMachineScriptUsage>
                                              scriptAnchorToStateMachineUsages,
                                          [NotNull]
                                          IDictionary<long, AnimatorStateMachineScriptUsage> stateMachineAnchorToUsage,
                                          [NotNull] IDictionary<long, long> childToParent)
        {
            GuidToAnchors = guidToAnchors;
            ScriptAnchorToStateUsages = scriptAnchorToStateUsages;
            ScriptAnchorToStateMachineUsages = scriptAnchorToStateMachineUsages;
            StateMachineAnchorToUsage = stateMachineAnchorToUsage;
            ChildToParent = childToParent;
        }

        public string ContainerId => nameof(AnimatorScriptUsagesElementContainer);

        public void AddData(object data)
        {
            switch (data)
            {
                case AnimatorStateScriptUsage animatorStateScriptUsage:
                    AddScriptToStateUsagesFor(animatorStateScriptUsage);
                    break;
                case AnimatorScript script:
                    AddScriptInfos(script);
                    break;
                case AnimatorStateMachineScriptUsage stateMachineScriptUsage:
                    AddStateMachineUsageInfosFor(stateMachineScriptUsage);
                    break;
            }
        }

        private void AddScriptToStateUsagesFor([NotNull] AnimatorStateScriptUsage animatorStateScriptUsage)
        {
            var scriptAnchorToStateUsages = ScriptAnchorToStateUsages;
            foreach (var anchor in animatorStateScriptUsage.ScriptsAnchors)
                scriptAnchorToStateUsages.Add(anchor, animatorStateScriptUsage);
        }

        private void AddScriptInfos(AnimatorScript script)
        {
            GuidToAnchors.Add(script.Guid, script.Anchor);
        }

        private void AddStateMachineUsageInfosFor([NotNull] AnimatorStateMachineScriptUsage stateMachineScriptUsage)
        {
            StateMachineAnchorToUsage[stateMachineScriptUsage.Location.LocalDocumentAnchor] = stateMachineScriptUsage;
            AddScriptToStateMachineUsagesFor(stateMachineScriptUsage);
            AddChildToParentEntriesFor(stateMachineScriptUsage);
        }

        private void AddScriptToStateMachineUsagesFor([NotNull] AnimatorStateMachineScriptUsage stateMachineScriptUsage)
        {
            var scriptToStateMachineUsages = ScriptAnchorToStateMachineUsages;
            foreach (var anchor in stateMachineScriptUsage.ScriptsAnchors)
                scriptToStateMachineUsages.Add(anchor, stateMachineScriptUsage);
        }

        private void AddChildToParentEntriesFor([NotNull] AnimatorStateMachineScriptUsage stateMachineScriptUsage)
        {
            var stateMachineAnchor = stateMachineScriptUsage.Location.LocalDocumentAnchor;
            AddChildStateToParentEntries(stateMachineScriptUsage, stateMachineAnchor);
            AddChildStateMachineToParentEntries(stateMachineScriptUsage, stateMachineAnchor);
        }

        private void AddChildStateMachineToParentEntries(
            [NotNull] AnimatorStateMachineScriptUsage stateMachineScriptUsage,
            long stateMachineAnchor)
        {
            foreach (var childStateAnchor in stateMachineScriptUsage.ChildStateMachinesAnchors)
            {
                if (ChildToParent.ContainsKey(childStateAnchor)) continue;
                ChildToParent[childStateAnchor] = stateMachineAnchor;
            }
        }

        private void AddChildStateToParentEntries([NotNull] AnimatorStateMachineScriptUsage stateMachineScriptUsage,
                                                  long stateMachineAnchor)
        {
            foreach (var childStateAnchor in stateMachineScriptUsage.ChildStatesAnchors)
            {
                if (ChildToParent.ContainsKey(childStateAnchor)) continue;
                ChildToParent[childStateAnchor] = stateMachineAnchor;
            }
        }

        private static object Read([NotNull] UnsafeReader reader)
        {
            var guidToAnchor = ReadGuidToAnchorsMap(reader);
            var anchorToStateUsagesMap = ReadAnchorToUsagesMap(reader, AnimatorStateScriptUsage.ReadFrom);
            var anchorToStateMachineUsagesMap = ReadAnchorToUsagesMap(reader, AnimatorStateMachineScriptUsage.ReadFrom);
            var stateMachineAnchorToUsageMap = ReadStateMachineAnchorToUsageMap(reader);
            var childToParent = ReadChildToParentMap(reader);
            return new AnimatorUsagesDataElement(guidToAnchor, anchorToStateUsagesMap, anchorToStateMachineUsagesMap,
                stateMachineAnchorToUsageMap, childToParent);
        }

        [NotNull]
        private static IDictionary<long, AnimatorStateMachineScriptUsage> ReadStateMachineAnchorToUsageMap(
            [NotNull] UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var stateMachineAnchorToUsage = new Dictionary<long, AnimatorStateMachineScriptUsage>(count);
            for (var i = 0; i < count; i++)
            {
                var anchor = reader.ReadLong();
                var usage = AnimatorStateMachineScriptUsage.ReadFrom(reader);
                stateMachineAnchorToUsage.Add(anchor, usage);
            }

            return stateMachineAnchorToUsage;
        }

        [NotNull]
        private static OneToListMap<Guid, long> ReadGuidToAnchorsMap([NotNull] UnsafeReader reader)
        {
            var guidToAnchorsCount = reader.ReadInt32();
            var anchorToUsages = new OneToListMap<Guid, long>(guidToAnchorsCount);
            for (var i = 0; i < guidToAnchorsCount; i++) ReadGuidToAnchors(reader, anchorToUsages);
            return anchorToUsages;
        }

        private static void ReadGuidToAnchors([NotNull] UnsafeReader reader,
                                              [NotNull] OneToListMap<Guid, long> guidToAnchors)
        {
            var guid = reader.ReadGuid();
            var usagesCount = reader.ReadInt32();
            for (var i = 0; i < usagesCount; i++) guidToAnchors.Add(guid, reader.ReadLong());
        }

        [NotNull]
        private static OneToListMap<long, T> ReadAnchorToUsagesMap<T>([NotNull] UnsafeReader reader,
                                                                      [NotNull] Func<UnsafeReader, T> read)
            where T : IAnimatorScriptUsage
        {
            var anchorToUsagesCount = reader.ReadInt32();
            var anchorToUsages = new OneToListMap<long, T>(anchorToUsagesCount);
            for (var i = 0; i < anchorToUsagesCount; i++) ReadAnchorToUsagesEntry(reader, anchorToUsages, read);
            return anchorToUsages;
        }

        private static void ReadAnchorToUsagesEntry<T>([NotNull] UnsafeReader reader,
                                                       [NotNull] OneToListMap<long, T> anchorToUsages,
                                                       [NotNull] Func<UnsafeReader, T> read)
            where T : IAnimatorScriptUsage
        {
            var anchor = reader.ReadLong();
            var usagesCount = reader.ReadInt32();
            for (var i = 0; i < usagesCount; i++) anchorToUsages.Add(anchor, read(reader));
        }

        [NotNull]
        private static IDictionary<long, long> ReadChildToParentMap([NotNull] UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var childToParent = new Dictionary<long, long>(count);
            for (var i = 0; i < count; i++)
            {
                var child = reader.ReadLong();
                var parent = reader.ReadLong();
                childToParent[child] = parent;
            }

            return childToParent;
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] AnimatorUsagesDataElement value)
        {
            if (writer is null || value is null) return;
            WriteGuidToAnchorsMap(writer, value.GuidToAnchors);
            WriteAnchorToUsagesMap(writer, value.ScriptAnchorToStateUsages);
            WriteAnchorToUsagesMap(writer, value.ScriptAnchorToStateMachineUsages);
            WriteStateMachineAnchorToUsageMap(writer, value.StateMachineAnchorToUsage);
            WriteChildToParentMap(writer, value.ChildToParent);
        }

        private static void WriteGuidToAnchorsMap([NotNull] UnsafeWriter writer,
                                                  [NotNull] OneToListMap<Guid, long> guidToAnchors)
        {
            writer.Write(guidToAnchors.Count);
            foreach (var (guid, anchors) in guidToAnchors)
            {
                if (anchors is null) continue;
                WriteGuidToAnchorsEntry(writer, guid, anchors);
            }
        }

        private static void WriteChildToParentMap([NotNull] UnsafeWriter writer,
                                                  [NotNull] IDictionary<long, long> childToParent)
        {
            writer.Write(childToParent.Count);
            foreach (var (child, parent) in childToParent)
            {
                writer.Write(child);
                writer.Write(parent);
            }
        }

        private static void WriteGuidToAnchorsEntry([NotNull] UnsafeWriter writer,
                                                    Guid guid,
                                                    [NotNull] ICollection<long> anchors)
        {
            writer.Write(guid);
            writer.Write(anchors.Count);
            foreach (var anchor in anchors) writer.Write(anchor);
        }


        private static void WriteAnchorToUsagesMap<T>([NotNull] UnsafeWriter writer,
                                                      [NotNull] OneToListMap<long, T> anchorToUsages)
            where T : IAnimatorScriptUsage
        {
            writer.Write(anchorToUsages.Count);
            foreach (var (anchor, usages) in anchorToUsages)
            {
                if (usages is null) continue;
                WriteAnchorToUsagesEntry(writer, anchor, usages);
            }
        }

        private static void WriteStateMachineAnchorToUsageMap<T>([NotNull] UnsafeWriter writer,
                                                                 [NotNull] IDictionary<long, T> anchorToUsages)
            where T : class, IAnimatorScriptUsage
        {
            writer.Write(anchorToUsages.Count);
            foreach (var (anchor, usage) in anchorToUsages)
            {
                if (usage is null) return;
                WriteTestAnchorToUsageEntry(writer, anchor, usage);
            }
        }

        private static void WriteTestAnchorToUsageEntry([NotNull] UnsafeWriter writer,
                                                        long anchor,
                                                        [NotNull] IAnimatorScriptUsage usage)
        {
            writer.Write(anchor);
            usage.WriteTo(writer);
        }

        private static void WriteAnchorToUsagesEntry<T>([NotNull] UnsafeWriter writer,
                                                        long anchor,
                                                        [NotNull] [ItemNotNull] ICollection<T> usages)
            where T : IAnimatorScriptUsage
        {
            writer.Write(anchor);
            writer.Write(usages.Count);
            foreach (var usage in usages) usage.WriteTo(writer);
        }
    }
}