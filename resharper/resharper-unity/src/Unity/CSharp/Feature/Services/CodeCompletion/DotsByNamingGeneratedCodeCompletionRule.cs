using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class DotsByNamingGeneratedCodeCompletionRule : DotsGeneratedCodeCompletionBaseRule
    {
        public override EvaluationMode SupportedEvaluationMode => EvaluationMode.Full;

        protected override void TransformItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            collector.RemoveWhere(lookupItem =>
            {
                var text = lookupItem.GetText();

                if (text.StartsWith("__"))
                {
                    if (text.StartsWith("__codegen"))
                        return true;
                    if (text[^1].IsHexDigitFast())
                        return true;
                }

                var declaredElement = lookupItem.GetPreferredDeclaredElement<ITypeMember>();

                if (declaredElement is CompiledElementBase compiledElementBase)
                {
                    return compiledElementBase.Module.Name.StartsWith("Unity") &&
                           compiledElementBase.HasMetadataAttributeInstances(KnownTypes.DOTSCompilerGenerated);
                }

                return false;
            });
        }
    }
}