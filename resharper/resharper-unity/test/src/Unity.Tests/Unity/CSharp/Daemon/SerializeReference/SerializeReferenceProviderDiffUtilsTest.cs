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

        private static ClassMetaInfo WithTypeParameter(string className, ElementId paramId, string paramName)
        {
            var typeParameters = new Dictionary<ElementId, TypeParameter>
            {
                { paramId, new TypeParameter(paramId, paramName, 0, new CountingSet<ElementId>()) }
            };
            return new ClassMetaInfo(className, new CountingSet<ElementId>(), new CountingSet<ElementId>(),
                typeParameters);
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
        public void RemovalOfAbsentType_DoesNotInsertGhostEntry()
        {
            var index = new IndexClassInfoDictionary();
            var foo = Id(1);
            var @base = Id(2);
            // A removal arrives for an id the index has never seen (stale/duplicated diff).
            var file = File((foo, WithSuper("Foo", @base)));

            Merge(index, file, null);
            Assert.That(index.ContainsKey(foo), Is.False,
                "ghost entry inserted for the removal of an absent type");
            Assert.That(index.ContainsKey(@base), Is.False);
        }

        [Test]
        public void PlaceholderSuperClass_DroppedWhenLastInheritorRemoved()
        {
            var index = new IndexClassInfoDictionary();
            var derived = Id(1);
            var @base = Id(2);
            var file = File((derived, WithSuper("Derived", @base)));

            Merge(index, null, file);
            Assert.That(index.ContainsKey(@base), Is.True, "referenced-only super class should get a placeholder");

            // Last inheritor removed: the now-empty placeholder must not leak.
            Merge(index, file, null);
            Assert.That(index.ContainsKey(derived), Is.False);
            Assert.That(index.ContainsKey(@base), Is.False,
                "empty placeholder leaked after its last inheritor was removed");
        }

        [Test]
        public void DeclaredSuperClass_SurvivesWhenLastInheritorRemoved()
        {
            var index = new IndexClassInfoDictionary();
            var derived = Id(1);
            var @base = Id(2);
            var baseFile = File((@base, Bare("Base")));
            var derivedFile = File((derived, WithSuper("Derived", @base)));

            Merge(index, null, baseFile);
            Merge(index, null, derivedFile);

            Merge(index, derivedFile, null);
            Assert.That(index.ContainsKey(derived), Is.False);
            Assert.That(index.ContainsKey(@base), Is.True,
                "declared type dropped when its last inheritor was removed");
        }

        [Test]
        public void StaleRemoval_DoesNotUnderflowDeclarationCount()
        {
            var index = new IndexClassInfoDictionary();
            var derived = Id(1);
            var @base = Id(2);
            var derivedFile = File((derived, WithSuper("Derived", @base)));
            var baseFile1 = File((@base, Bare("Base")));
            var baseFile2 = File((@base, Bare("Base")));
            var staleBaseRemoval = File((@base, WithSuper("Base", Id(3))));

            // Base exists only as a referenced placeholder (declaration count 0).
            Merge(index, null, derivedFile);
            // A stale removal hits the placeholder - must not push the count below zero.
            Merge(index, staleBaseRemoval, null);
            Assert.That(index.ContainsKey(@base), Is.True);

            // Two files start declaring Base, then one stops: Base must survive...
            Merge(index, null, baseFile1);
            Merge(index, null, baseFile2);
            Merge(index, baseFile1, null);

            // ...even after its last relationship is gone, because baseFile2 still declares it.
            Merge(index, derivedFile, null);
            Assert.That(index.ContainsKey(@base), Is.True,
                "declaration count underflow dropped the entry while a file still declares it");

            Merge(index, baseFile2, null);
            Assert.That(index.ContainsKey(@base), Is.False);
        }

        [Test]
        public void RemovalOfAbsentBareType_DegradesToNoOp()
        {
            var index = new IndexClassInfoDictionary();
            var foo = Id(1);
            var file = File((foo, Bare("Foo")));

            // An empty Removed diff for an id the index has never seen used to trip the
            // "!classMetaInfoDiff.IsEmpty() || diff.DiffType == DiffType.Added" assertion.
            Merge(index, file, null);
            Assert.That(index, Is.Empty);
        }

        [Test]
        public void TypeParameterRemovalForAbsentEntry_DegradesToNoOp()
        {
            var index = new IndexClassInfoDictionary();
            var generic = Id(1);
            var typeParam = Id(2);
            var file = File((generic, WithTypeParameter("Generic", typeParam, "T")));

            // A Removed type-parameter diff for an absent entry used to trip the
            // "diff.DiffType == DiffType.Added" assertion in ProcessTypeParametersDiff.
            Merge(index, file, null);
            Assert.That(index, Is.Empty);
        }

        [Test]
        public void PartialGenericClass_TypeParameterSurvivesUntilLastDeclaringFileRemoved()
        {
            var index = new IndexClassInfoDictionary();
            var generic = Id(1);
            var typeParam = Id(2);
            // Two files declaring the same partial generic class -> same type parameter id.
            var file1 = File((generic, WithTypeParameter("Generic", typeParam, "T")));
            var file2 = File((generic, WithTypeParameter("Generic", typeParam, "T")));

            Merge(index, null, file1);
            Merge(index, null, file2);
            Assert.That(index.ContainsKey(typeParam), Is.True);

            Merge(index, file1, null);
            Assert.That(index.ContainsKey(generic), Is.True);
            Assert.That(index.ContainsKey(typeParam), Is.True,
                "type parameter dropped while another file still declares it");

            Merge(index, file2, null);
            Assert.That(index.ContainsKey(generic), Is.False);
            Assert.That(index.ContainsKey(typeParam), Is.False);
        }

        [Test]
        public void ClassNameCollisionOnApply_SkipsElementInsteadOfThrowing()
        {
            var index = new IndexClassInfoDictionary();
            var id = Id(1);
            var @base = Id(2);
            var fooFile = File((id, Bare("Foo")));
            var barFile = File((id, WithSuper("Bar", @base)));

            Merge(index, null, fooFile);

            // Same id, different FQN (id collision): the diff must be skipped, not thrown.
            // An error is logged intentionally. The assembly-level trap collects separately from the
            // fixture one, and rethrows into whichever test tears down next - pause both.
            using (TestEnvironmentAssembly.PauseCollectingExceptions())
            using (PauseCollectingExceptions())
                Merge(index, null, barFile);

            Assert.That(index[id].ClassName, Is.EqualTo("Foo"), "colliding diff was applied to the wrong class");
            Assert.That(index.ContainsKey(@base), Is.False, "relationships of a skipped colliding diff leaked");

            // The skipped diff must not have affected the declaration count.
            Merge(index, fooFile, null);
            Assert.That(index.ContainsKey(id), Is.False);
        }

        [Test]
        public void ClassNameCollisionOnCalculateDiff_SkipsElementInsteadOfThrowing()
        {
            var index = new IndexClassInfoDictionary();
            var id = Id(1);
            var fooFile = File((id, Bare("Foo")));
            var barFile = File((id, Bare("Bar")));

            Merge(index, null, fooFile);

            // Old and new file data disagree on the FQN under one id: used to throw ArgumentException.
            // See ClassNameCollisionOnApply_SkipsElementInsteadOfThrowing on why both traps are paused.
            using (TestEnvironmentAssembly.PauseCollectingExceptions())
            using (PauseCollectingExceptions())
                Merge(index, fooFile, barFile);

            Assert.That(index[id].ClassName, Is.EqualTo("Foo"));

            Merge(index, fooFile, null);
            Assert.That(index.ContainsKey(id), Is.False);
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

        // Same seam UnitySerializedReferenceInfoIndex.Merge uses for the TypeParameterResolves path.
        private static void ApplyResolve(IndexClassInfoDictionary index, TypeParameterResolve resolve,
            DiffType diffType)
        {
            SerializeReferenceProviderDiffUtils.ApplyDiff(index,
                new List<TypeParameterResolvesDiff> { new(resolve, diffType, 1) });
        }

        [Test]
        public void TypeParameterResolve_AddThenRemove_DropsPlaceholders()
        {
            var index = new IndexClassInfoDictionary();
            var openType = Id(1);
            var resolvedType = Id(2);
            var resolve = new TypeParameterResolve("T", openType, resolvedType);

            ApplyResolve(index, resolve, DiffType.Added);
            Assert.That(index[openType].Inheritors.GetCount(resolvedType), Is.EqualTo(1));
            Assert.That(index[resolvedType].SuperClasses.GetCount(openType), Is.EqualTo(1));

            ApplyResolve(index, resolve, DiffType.Removed);
            Assert.That(index, Is.Empty, "resolve placeholders leaked after the resolve was removed");
        }

        [Test]
        public void SelfResolution_AddThenRemove_DoesNotThrow()
        {
            var index = new IndexClassInfoDictionary();
            var id = Id(1);
            // OpenTypeId == ResolvedTypeId: the open-type add inserts the very key the resolved-type
            // lookup is about to check - used to trip the alreadyHasSameKey assertion.
            var resolve = new TypeParameterResolve("T", id, id);

            ApplyResolve(index, resolve, DiffType.Added);
            Assert.That(index.ContainsKey(id), Is.True);
            Assert.That(index[id].Inheritors.GetCount(id), Is.EqualTo(1));
            Assert.That(index[id].SuperClasses.GetCount(id), Is.EqualTo(1));

            ApplyResolve(index, resolve, DiffType.Removed);
            Assert.That(index, Is.Empty);
        }
    }
}
