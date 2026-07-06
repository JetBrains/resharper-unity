using System.Collections.Generic;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference;
using JetBrains.TestFramework;
using JetBrains.Util.Collections;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    // Unit tests for the incremental merge (CalculateDiff + ApplyDiff) that accumulates the global
    // [SerializeReference] type index. The solution-level gold tests only cover the first, all-added
    // analysis; these cover the Removed-diff / partial-class lifecycle where RIDER crashes occurred:
    // a type declared by several files must survive until the LAST declaring file removes it.
    [TestFixture]
    public class SerializeReferenceProviderDiffUtilsTest : BaseTestNoShell
    {
        // Same seam UnitySerializedReferenceInfoIndex.Merge uses for the TypeToInterfaces path.
        private static void Merge(IndexClassInfoDictionary index, ClassMetaInfoDictionary? oldData,
            ClassMetaInfoDictionary? newData)
        {
            SerializeReferenceProviderDiffUtils.MergeTypeToInterfaces(index, oldData, newData);
        }

        // ElementId clears bit 0 for source elements, so double the value to keep ids distinct.
        private static ElementId Id(int value) => new(value * 2, isCompiledElement: false);

        private static ClassMetaInfoDictionary File(params (ElementId id, ClassMetaInfo info)[] entries)
        {
            var dict = new ClassMetaInfoDictionary();
            foreach (var (id, info) in entries)
                dict.Add(id, info);
            return dict;
        }

        private static ClassMetaInfo Bare(string className) => new(className);

        private static ClassMetaInfo WithSuper(string className, ElementId superId)
        {
            var superClasses = new CountingSet<ElementId>();
            superClasses.Add(superId, 1);
            return new ClassMetaInfo(className, superClasses, new CountingSet<ElementId>(),
                new Dictionary<ElementId, TypeParameter>());
        }

        [Test]
        public void SingleFile_AddThenRemove_DropsEntry()
        {
            var index = new IndexClassInfoDictionary();
            var foo = Id(1);
            var file = File((foo, Bare("Foo")));

            Merge(index, null, file);
            Assert.That(index.ContainsKey(foo), Is.True);

            Merge(index, file, null);
            Assert.That(index.ContainsKey(foo), Is.False);
        }

        [Test]
        public void PartialClass_SurvivesUntilLastDeclaringFileRemoved()
        {
            var index = new IndexClassInfoDictionary();
            var foo = Id(1);
            // Two files declaring the same (bare) partial class -> same ElementId.
            var file1 = File((foo, Bare("Foo")));
            var file2 = File((foo, Bare("Foo")));

            Merge(index, null, file1);
            Merge(index, null, file2);
            Assert.That(index.ContainsKey(foo), Is.True);

            // Removing the first part must NOT drop the entry - the second part still declares it.
            Merge(index, file1, null);
            Assert.That(index.ContainsKey(foo), Is.True,
                "partial type dropped while another file still declares it");

            // Removing the last part drops the entry (and must not hit an assertion on a missing entry).
            Merge(index, file2, null);
            Assert.That(index.ContainsKey(foo), Is.False,
                "partial type not removed after its last declaration was removed");
        }

        [Test]
        public void Reanalysis_OfUnchangedFile_DoesNotInflateDeclarationCount()
        {
            var index = new IndexClassInfoDictionary();
            var foo = Id(1);
            var version1 = File((foo, Bare("Foo")));
            var version2 = File((foo, Bare("Foo")));

            Merge(index, null, version1);   // initial analysis
            Merge(index, version1, version2); // re-analysis, content unchanged (old and new both present)
            Assert.That(index.ContainsKey(foo), Is.True);

            // A single removal must drop it: re-analysis should not have counted a second declaration.
            Merge(index, version2, null);
            Assert.That(index.ContainsKey(foo), Is.False,
                "re-analysis of an unchanged file inflated the declaration count");
        }

        [Test]
        public void PartialClassWithSuperClass_TracksRelationshipsAcrossDeclarations()
        {
            var index = new IndexClassInfoDictionary();
            var derived = Id(1);
            var @base = Id(2);
            var file1 = File((derived, WithSuper("Derived", @base)));
            var file2 = File((derived, WithSuper("Derived", @base)));

            Merge(index, null, file1);
            Merge(index, null, file2);
            Assert.That(index[derived].SuperClasses.GetCount(@base), Is.EqualTo(2));
            Assert.That(index[@base].Inheritors.GetCount(derived), Is.EqualTo(2));

            // One part removed: entry and relationships survive, counts drop by one.
            Merge(index, file1, null);
            Assert.That(index.ContainsKey(derived), Is.True, "derived dropped while a partial part remains");
            Assert.That(index[derived].SuperClasses.GetCount(@base), Is.EqualTo(1));
            Assert.That(index[@base].Inheritors.GetCount(derived), Is.EqualTo(1));

            // Last part removed: the declared type is gone.
            Merge(index, file2, null);
            Assert.That(index.ContainsKey(derived), Is.False);
        }
    }
}
