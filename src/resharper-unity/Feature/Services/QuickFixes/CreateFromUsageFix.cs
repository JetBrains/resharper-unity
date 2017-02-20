using System.Collections.Generic;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Intentions.CreateFromUsage;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes
{
    [QuickFix]
    public class CreateFromUsageFix : CreateFromUsageFixBase
    {
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
            UnfilteredItems = new List<ICreateFromUsageAction>();

            var treeNode = reference.GetTreeNode();
            if (treeNode.IsFromUnityProject())
                UnfilteredItems.Add(new CreateSerializedFieldFromUsageAction(reference));
        }
    }
}