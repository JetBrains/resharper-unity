using System.IO;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.TestFramework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public abstract class ShaderLabCodeCompletionTestBase : CodeCompletionTestBase
    {
        protected override void PresentLookupItem(TextWriter writer, ILookupItem lookupItem, bool showTypes)
        {
            base.PresentLookupItem(writer, lookupItem, showTypes);

            if (lookupItem is LookupItem { ItemInfo: TextualInfo textualInfo })
            {
                var tailType = textualInfo.TailType;
                if (tailType != null && tailType != TailType.None)
                    writer.Write("(" + tailType + ")");
            }
        }

        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

    }
}