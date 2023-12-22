using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class DotsMembersGeneratedCodeCompletionRule : DotsGeneratedCodeCompletionBaseRule
    {
        protected override void TransformItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            collector.RemoveWhere(lookupItem =>
            {
                var typeElement = context.GetData(HighlightMembersOfCurrentClassRule.OwnerTypeElementKey);
                if (typeElement == null) 
                    return false;
                
                if (!typeElement.IsDotsImplicitlyUsedType())
                    return false;
                
                var declaredElement = lookupItem.GetPreferredDeclaredElement<ITypeMember>();
                if (declaredElement is not CachedTypeMemberBase typeMember)
                    return false;
                
                var memberSourceFile = typeMember.GetSingleOrDefaultSourceFile();
                if (memberSourceFile == null) 
                    return false;

                return typeMember is IDeclaredElement element && element.ShortName.StartsWith("__") &&
                       memberSourceFile.IsSourceGeneratedFile() && typeMember.ContainingType.IsDotsImplicitlyUsedType();
            });
        }
    }
}