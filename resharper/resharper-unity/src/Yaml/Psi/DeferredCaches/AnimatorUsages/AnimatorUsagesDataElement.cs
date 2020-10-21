using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Serialization;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    [PolymorphicMarshaller]
    public class AnimatorUsagesDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as AnimatorUsagesDataElement);
        [NotNull] public readonly OneToListMultimap<long, AnimatorStateScriptUsage> ScriptAnchorToStateUsages;
        [NotNull] public readonly OneToListMultimap<long, AnimatorStateMachineScriptUsage> ScriptAnchorToStateMachineUsages;
        [NotNull] public readonly OneToListMultimap<Guid, AnimatorStateScriptUsage> GuidToStateUsages;
        [NotNull] public readonly OneToListMultimap<Guid, AnimatorStateMachineScriptUsage> GuidToStateMachineUsages;
        [NotNull] public readonly IDictionary<long, AnimatorStateMachineScriptUsage> StateMachineAnchorToUsage;
        [NotNull] public readonly IDictionary<long, long> ChildToParent;
        [NotNull] public readonly List<AnimatorScript> Scripts;

        public AnimatorUsagesDataElement()
        {
            Scripts = new List<AnimatorScript>(1);
            ScriptAnchorToStateUsages = new OneToListMultimap<long, AnimatorStateScriptUsage>();
            ScriptAnchorToStateMachineUsages = new OneToListMultimap<long, AnimatorStateMachineScriptUsage>();
            StateMachineAnchorToUsage = new Dictionary<long, AnimatorStateMachineScriptUsage>();
            GuidToStateUsages = new OneToListMultimap<Guid, AnimatorStateScriptUsage>();
            GuidToStateMachineUsages = new OneToListMultimap<Guid, AnimatorStateMachineScriptUsage>();
            ChildToParent = new Dictionary<long, long>();
        }
        
        private AnimatorUsagesDataElement([NotNull] List<AnimatorScript> scripts,
                                          [NotNull] OneToListMultimap<long, AnimatorStateScriptUsage> scriptAnchorToStateUsages,
                                          [NotNull] OneToListMultimap<long, AnimatorStateMachineScriptUsage> scriptAnchorToStateMachineUsages,
                                          [NotNull] IDictionary<long, AnimatorStateMachineScriptUsage> stateMachineAnchorToUsage,
                                          [NotNull] OneToListMultimap<Guid, AnimatorStateScriptUsage> guidToStateUsages,
                                          [NotNull] OneToListMultimap<Guid, AnimatorStateMachineScriptUsage> guidToStateMachineUsages,
                                          [NotNull] IDictionary<long, long> childToParent)
        {
            Scripts = scripts;
            ScriptAnchorToStateUsages = scriptAnchorToStateUsages;
            ScriptAnchorToStateMachineUsages = scriptAnchorToStateMachineUsages;
            StateMachineAnchorToUsage = stateMachineAnchorToUsage;
            GuidToStateUsages = guidToStateUsages;
            GuidToStateMachineUsages = guidToStateMachineUsages;
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
            Scripts.Add(script);
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

        private void AddChildStateMachineToParentEntries([NotNull] AnimatorStateMachineScriptUsage stateMachineScriptUsage,
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
            var scripts = ReadScripts(reader);
            var anchorToStateUsagesMap = ReadAnchorToUsagesMap(reader, AnimatorStateScriptUsage.ReadFrom);
            var anchorToStateMachineUsagesMap = ReadAnchorToUsagesMap(reader, AnimatorStateMachineScriptUsage.ReadFrom);
            var stateMachineAnchorToUsageMap = ReadStateMachineAnchorToUsageMap(reader);
            var guidToStateUsages = ReadGuidToUsagesMap(reader, AnimatorStateScriptUsage.ReadFrom);
            var guidToStateMachineUsages = ReadGuidToUsagesMap(reader, AnimatorStateMachineScriptUsage.ReadFrom);
            var childToParent = ReadChildToParentMap(reader);
            return new AnimatorUsagesDataElement(scripts, anchorToStateUsagesMap, anchorToStateMachineUsagesMap,
                stateMachineAnchorToUsageMap, guidToStateUsages, guidToStateMachineUsages, childToParent);
        }

        [NotNull]
        private static List<AnimatorScript> ReadScripts([NotNull] UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var scripts = new List<AnimatorScript>(count);
            for (var i = 0; i < count; i++)
            {
                var anchor = reader.ReadLong();
                var guid = reader.ReadGuid();
                scripts.Add(new AnimatorScript(guid, anchor));
            }
            return scripts;
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
        private static OneToListMultimap<long, T> ReadAnchorToUsagesMap<T>([NotNull] UnsafeReader reader,
                                                                           [NotNull] Func<UnsafeReader, T> read)
                                                                           where T : IAnimatorScriptUsage
        {
            var anchorToUsagesCount = reader.ReadInt32();
            var anchorToUsages = new OneToListMultimap<long, T>(anchorToUsagesCount);
            for (var i = 0; i < anchorToUsagesCount; i++) ReadAnchorToUsagesEntry(reader, anchorToUsages, read);
            return anchorToUsages;
        }

        private static void ReadAnchorToUsagesEntry<T>([NotNull] UnsafeReader reader, 
                                                       [NotNull] OneToListMultimap<long, T> anchorToUsages,
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

        [NotNull]
        private static OneToListMultimap<Guid, T> ReadGuidToUsagesMap<T>(
            [NotNull] UnsafeReader reader,
            [NotNull] Func<UnsafeReader, T> read)
            where T : IAnimatorScriptUsage
        {
            var guidToUsagesCount = reader.ReadInt32();
            var guidToUsages = new OneToListMultimap<Guid, T>(guidToUsagesCount);
            for (var i = 0; i < guidToUsagesCount; i++) ReadGuidToUsagesEntry(reader, guidToUsages, read);
            return guidToUsages;
        }

        private static void ReadGuidToUsagesEntry<T>([NotNull] UnsafeReader reader,
                                                     [NotNull] OneToListMultimap<Guid, T> guidToUsages,
                                                     [NotNull] Func<UnsafeReader, T> read)
            where T : IAnimatorScriptUsage
        {
            var guid = reader.ReadGuid();
            var usagesCount = reader.ReadInt32();
            for (var i = 0; i < usagesCount; i++) guidToUsages.Add(guid, read(reader));
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] AnimatorUsagesDataElement value)
        {
            if (writer is null || value is null) return;
            WriteScripts(writer, value.Scripts);
            WriteAnchorToUsagesMap(writer, value.ScriptAnchorToStateUsages);
            WriteAnchorToUsagesMap(writer, value.ScriptAnchorToStateMachineUsages);
            WriteStateMachineAnchorToUsageMap(writer, value.StateMachineAnchorToUsage);
            WriteGuidToUsagesMap(writer, value.GuidToStateUsages);
            WriteGuidToUsagesMap(writer, value.GuidToStateMachineUsages);
            WriteChildToParentMap(writer, value.ChildToParent);
        }

        private static void WriteScripts([NotNull] UnsafeWriter writer,
                                         [NotNull] IReadOnlyCollection<AnimatorScript> scripts)
        {
            writer.Write(scripts.Count);
            foreach (var script in scripts)
            {
                writer.Write(script.Anchor);
                writer.Write(script.Guid);
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
        
        
        private static void WriteAnchorToUsagesMap<T>([NotNull] UnsafeWriter writer,
                                                      [NotNull] OneToListMultimap<long, T> anchorToUsages)
            where T : IAnimatorScriptUsage
        {
            writer.Write(anchorToUsages.Count);
            foreach (var (anchor, usages) in anchorToUsages)
            {
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
                                                        [ItemNotNull] MultimapValueCollection<long, T> usages)
            where T : IAnimatorScriptUsage
        {
            writer.Write(anchor);
            writer.Write(usages.Count);
            foreach (var usage in usages) usage.WriteTo(writer);
        }

        private static void WriteGuidToUsagesMap<T>([NotNull] UnsafeWriter writer,
                                                    [NotNull] OneToListMultimap<Guid, T> guidToUsages)
            where T : IAnimatorScriptUsage
        {
            writer.Write(guidToUsages.Count);
            foreach (var (guid, usages) in guidToUsages) WriteGuidToUsagesEntry(writer, guid, usages);
        }

        private static void WriteGuidToUsagesEntry<T>([NotNull] UnsafeWriter writer,
                                                      Guid guid,
                                                      [ItemNotNull] MultimapValueCollection<Guid, T> usages)
            where T : IAnimatorScriptUsage
        {
            writer.Write(guid);
            writer.Write(usages.Count);
            foreach (var usage in usages) usage.WriteTo(writer);
        }
    }
}