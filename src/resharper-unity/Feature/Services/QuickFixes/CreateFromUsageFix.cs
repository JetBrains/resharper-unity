using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

#if WAVE08
using JetBrains.UI.BulbMenu;
#else
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Positions;
#endif

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class CreateFromUsageFix : IQuickFix
    {
        // CreateFromUsageFixBase will add items as unsorted. They will appear in the order in
        // which they are added. That means its impossible to add something to the end of the
        // last (ordering means we go first). If we create a child anchor, that is immediately
        // sorted, so appears at the top, no matter what we do. The only thing we can do is
        // create a sibling group, which means we appear straight after the normal create group.
        // The submenu is pushed below us because it doesn't have a position, so is unsorted.
        // Its items are also added unsorted, so we can't add anything without it going to the
        // top of the list.
        private static readonly InvisibleAnchor AfterCreateFromUsageAnchor =
            CreateFromUsageFixBase.CreateFromUsageAnchor.CreateNext();

        private static readonly InvisibleAnchor AfterCreateFromUsageOthersAnchor = new InvisibleAnchor(
            CreateFromUsageFixBase.CreateFromUsageOthersAnchor, AnchorPosition.BasePosition.GetNext());

        private readonly List<ICreateFromUsageAction> myUnfilteredItems;

        // Unresolved variable/field name
        public CreateFromUsageFix(NotResolvedError error)
            : this(error.Reference)
        {
        }

        // Not sure what causes this, but ReSharper's own create field uses it
        public CreateFromUsageFix(AccessRightsError error)
            : this(error.Reference)
        {
        }

        // E.g. using a type name like a variable/field name
        public CreateFromUsageFix(UnexpectedElementTypeError error)
            : this(error.Reference)
        {
        }

        private CreateFromUsageFix(IReference reference)
        {
            myUnfilteredItems = new List<ICreateFromUsageAction>();

            var treeNode = reference.GetTreeNode();
            if (treeNode.IsFromUnityProject())
                myUnfilteredItems.Add(new CreateSerializedFieldFromUsageAction(reference));
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            IEnumerable<IBulbAction> firstLevelItems, secondLevelItems;
            GetItems(out firstLevelItems, out secondLevelItems);
            return firstLevelItems.Any() || secondLevelItems.Any();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            IEnumerable<IBulbAction> firstLevelItems, secondLevelItems;
            GetItems(out firstLevelItems, out secondLevelItems);

            // This is where we differ from CreateFromUsageFixBase. Don't promote anything from the "create other" menu
            foreach (var firstLevelItem in firstLevelItems)
                yield return new IntentionAction(firstLevelItem, null, AfterCreateFromUsageAnchor);

            foreach (var secondLevelItem in secondLevelItems)
                yield return new IntentionAction(secondLevelItem, null, AfterCreateFromUsageOthersAnchor);
        }

        private void GetItems(out IEnumerable<IBulbAction> firstLevelItems, out IEnumerable<IBulbAction> secondLevelItems)
        {
            var firstLevelItemsList = new List<IBulbAction>();
            var secondLevelItemsList = new List<IBulbAction>();

            var unorderedItems = myUnfilteredItems.Select(a => Tuple.Create(a, a.GetBulbItem()))
                .Where(i => i.Item2 != null)
                .ToList();

            var consistencyGroupToBulbItem = new OneToListMap<ICreatedElementConsistencyGroup, IBulbAction>();
            foreach (var unorderedItem in unorderedItems)
                consistencyGroupToBulbItem.Add(unorderedItem.Item1.GetConsistencyGroup(), unorderedItem.Item2);

            foreach (var consistencyGroup in consistencyGroupToBulbItem.Keys)
            {
                InterruptableActivityCookie.CheckAndThrow();

                var bulbItems = consistencyGroupToBulbItem[consistencyGroup];

                if (consistencyGroup.IsConsistent())
                    firstLevelItemsList.AddRange(bulbItems);
                else
                    secondLevelItemsList.AddRange(bulbItems);
            }

            firstLevelItems = firstLevelItemsList;
            secondLevelItems = secondLevelItemsList;
        }
    }
}