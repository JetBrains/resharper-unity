using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstReferenceExpressionAnalyzer : BurstProblemAnalyzerBase<IReferenceExpression>
    {
        protected override bool CheckAndAnalyze(IReferenceExpression referenceExpression, IHighlightingConsumer consumer)
        {
            var element = referenceExpression.Reference.Resolve().DeclaredElement;

            //here I want to handle next situations
            //1. accessing typemembers, whether static or not, including: properties, fields, localVariable, Parameters

            //non auto property are not interested cuz they are not prohibited,
            //and any backing field will be handled inside accessor 
            if (element is IProperty property && property.IsAuto || element is IField)
            {
                var typeMember = (ITypeMember) element;
                if (referenceExpression.GetAccessType().HasFlag(ExpressionAccessType.Read) &&
                    typeMember.IsStatic &&
                    !typeMember.IsReadonly &&
                    !typeMember.IsConstant() &&
                    !typeMember.IsEnumMember() &&
                    !(typeMember is IProperty prop && !prop.IsWritable && prop.IsReadable))
                {
                    consumer?.AddHighlighting(new BC1042Error(referenceExpression.GetDocumentRange(),
                        typeMember.GetContainingType()?.ShortName, element.ShortName));
                    return true;
                }
                if (referenceExpression.GetAccessType().HasFlag(ExpressionAccessType.Write) && typeMember.IsStatic)
                {
                    //there are no static write-only auto properties
                    var field = element.ShortName;
                    if (element is IProperty)
                        field += "__backing_field";
                    consumer?.AddHighlighting(new BC1034Error(referenceExpression.GetDocumentRange(), field));
                    return true;
                }
            }
            
            if (element is ITypeOwner typeOwner)
            {
                if (element is IModifiersOwner modifiersOwner &&
                    (modifiersOwner.IsVirtual || modifiersOwner.IsOverride || modifiersOwner.IsAbstract))
                {
                    //virtual and abstract cannot be in struct. only override is getHashCode -> function
                    consumer?.AddHighlighting(new BC1042Error(referenceExpression.GetDocumentRange(),
                        typeOwner.Type().GetTypeElement()?.ShortName, element.ShortName));
                    return true;
                }

                if (!typeOwner.Type().IsBurstPermittedType())
                {
                    if(typeOwner is IAttributesOwner attributesOwner && attributesOwner.HasAttributeInstance(KnownTypes.NativeSetClassTypeToNullOnScheduleAttribute, AttributesSource.Self))
                        return false;
                    
                    consumer?.AddHighlighting(new BC1042ShortError(referenceExpression.GetDocumentRange(),
                        typeOwner.Type().GetTypeElement()?.ShortName));
                    return true;
                }
            }

            return false;
        }
    }
}