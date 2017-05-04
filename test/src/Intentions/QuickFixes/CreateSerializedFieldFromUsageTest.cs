using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Intentions.Legacy;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using JetBrains.TextControl;
using JetBrains.Util.Special;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    public class CreateSerializedFieldFromUsageTest
        : CSharpCreateFromUsageTestBase<CreateSerializedFieldFromUsageAction>
    {
        protected override string RelativeTestDataPath => @"intentions\QuickFixes\CreateFromUsage";

        [Test] public void TestSerializedField01() { DoNamedTest2(); }
        [Test] public void TestSerializedField02() { DoNamedTest2(); }
        [Test] public void TestSerializedField03() { DoNamedTest2(); }
        [Test] public void TestSerializedField04() { DoNamedTest2(); }
    }

    public abstract class CSharpCreateFromUsageTestBase<T> : CSharpQuickFixTestBase<CreateFromUsageFix>
        where T : ICreateFromUsageAction
    {
        protected override Func<IList<IntentionAction>, IBulbAction> GetBulbItemSelector(ITextControl textControl)
        {
            return menu =>
                menu.Select(a =>
                        (a.BulbAction as PartSelectionBulbItemProxy).IfNotNull(
                            proxy => proxy.UnproxyItem(GetPartSelectionNumber()), a.BulbAction)).
                    Single(_ => _.GetType() == typeof(T));
        }
    }
}