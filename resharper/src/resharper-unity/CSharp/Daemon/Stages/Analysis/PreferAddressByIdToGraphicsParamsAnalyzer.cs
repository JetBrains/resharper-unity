using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Validation;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(typeof(IInvocationExpression), HighlightingTypes = new[] { typeof(PreferAddressByIdToGraphicsParamsWarning) })]
    public class PreferAddressByIdToGraphicsParamsAnalyzer : UnityElementProblemAnalyzer<IInvocationExpression>
    {
        
        private static readonly IDictionary<IClrTypeName, (IClrTypeName, string)> ourTypes = new Dictionary<IClrTypeName, (IClrTypeName, string)>()
        {
            {KnownTypes.Animator, (KnownTypes.Animator, "StringToHash")},
            {KnownTypes.Shader, (KnownTypes.Shader, "PropertyToID")},
            {KnownTypes.Material, (KnownTypes.Shader, "PropertyToID")},
        }; 
        
        public PreferAddressByIdToGraphicsParamsAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }

        protected override void Analyze(IInvocationExpression expression, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var reference = expression.Reference;
            if (reference == null) 
                return;
            
            var info = reference.Resolve();
            if (info.ResolveErrorType == ResolveErrorType.OK && info.DeclaredElement is IMethod stringMethod)
            {
                if (HasOverloadWithIntParameter(stringMethod, expression, out var argumentIndex, out var containingType))
                {
                    var argument = expression.Arguments[argumentIndex];
                    var (clrName, methodName) = ourTypes[containingType.GetClrName()];
                    var literal = (argument.Expression as ILiteralExpression)?.ConstantValue.Value as string;
                    consumer.AddHighlighting(new PreferAddressByIdToGraphicsParamsWarning(expression, argument, literal, clrName.FullName, methodName));
                }
            }
        }

        private bool HasOverloadWithIntParameter(IMethod stringMethod, IInvocationExpression expression, out int index, out ITypeElement containingType)
        {
            index = 0;
            containingType = null;
            
            if (!stringMethod.ShortName.StartsWith("Get") && !stringMethod.ShortName.StartsWith("Set")) return false;
            
            containingType = stringMethod.GetContainingType();

            if (containingType == null || !ourTypes.ContainsKey(containingType.GetClrName()))
                return false;
            
            var type = TypeFactory.CreateType(containingType);
            var table = type.GetSymbolTable(expression.PsiModule).Filter(
                new AccessRightsFilter(new DefaultAccessContext(expression)),
                new ExactNameFilter(stringMethod.ShortName),
                new PredicateFilter(t => MatchSignatureStringToIntMethod(stringMethod, t.GetDeclaredElement() as IMethod)));


            if (table.GetSymbolInfos(stringMethod.ShortName).SingleOrDefault()?.GetDeclaredElement() is IMethod result)
            {
                var parameters = result.Parameters;
                var stringMethodParameters = stringMethod.Parameters;
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].Type.IsInt() && stringMethodParameters[i].Type.IsString())
                    {
                        index = i;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool MatchSignatureStringToIntMethod(IMethod stringMethod, IMethod intMethod)
        {
            if (!stringMethod.ReturnType.Equals(intMethod.ReturnType)) return false;

            var stringMethodParameters = stringMethod.Parameters;
            var intMethodParameters = intMethod.Parameters;

            if (stringMethodParameters.Count != intMethodParameters.Count) return false;

            bool isFound = false;
            for (int i = 0; i < stringMethodParameters.Count; i++)
            {
                var intMethodParam = intMethodParameters[i];
                var stringMethodParam = stringMethodParameters[i];

                if (stringMethodParam.Type.IsString() && intMethodParam.Type.IsInt())
                {
                    if (!intMethodParam.ShortName.ToLower().Contains("id")||
                        !stringMethodParam.ShortName.ToLower().Contains("name"))
                        return false;
                    
                    isFound = true;
                }
                else if (!intMethodParam.Type.Equals(stringMethodParam.Type))
                {
                    return false;
                }
            }

            return isFound;
        }
    }
}