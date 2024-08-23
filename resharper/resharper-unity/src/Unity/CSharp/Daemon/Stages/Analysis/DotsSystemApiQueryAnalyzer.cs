#nullable enable
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis;

[ElementProblemAnalyzer(Instantiation.DemandAnyThreadUnsafe, typeof(IInvocationExpression),
        HighlightingTypes = [typeof(MustBeSurroundedWithRefRwRoWarning)]
    )
]
public class DotsSystemApiQueryAnalyzer(UnityApi unityApi)
    : UnityElementProblemAnalyzer<IInvocationExpression>(unityApi)
{
    protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data,
        IHighlightingConsumer consumer)
    {
        if (!expression.IsSystemApiQuery())
            return;
        
        if(expression.InvokedExpression is not IReferenceExpression referenceExpression)
            return;

        var typeArgumentList = referenceExpression.TypeArgumentList;

        for (var index = 0; index < typeArgumentList.TypeArguments.Count; index++)
        {
            var typeArgument = typeArgumentList.TypeArguments[index];
            
            if (typeArgument is not IDeclaredType declaredType)
                continue;

            var typeElement = declaredType.GetTypeElement();
            if (typeElement == null)
                continue;

            //skips analysis if the wrong type is used
            if (!typeElement.DerivesFrom(KnownTypes.IQueryTypeParameter))
                continue;

            //It's allowed enumerating over all Aspects, RefRO, and RefRW of a given type.
            if (typeElement.IsClrName(KnownTypes.RefRW)
                || typeElement.IsClrName(KnownTypes.RefRO)
                || typeElement.DerivesFrom(KnownTypes.IAspect))
                continue;

            var typeArgumentNode = typeArgumentList.TypeArgumentNodes[index];
            consumer.AddHighlighting(new MustBeSurroundedWithRefRwRoWarning(typeArgumentNode!, index));
        }
    }

    public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
    {
        return base.ShouldRun(file, data)
               && DotsUtils.IsUnityProjectWithEntitiesPackage(file);
    }
}