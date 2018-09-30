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
        
        // The map from IClrTypeName to help method and his IClrTypeName
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

            var arguments = expression.Arguments;
            
            // cheap check for uninteresting methods
            if (expression.TypeArguments.Count != 0)
                return;

            if (arguments.Count == 0)
                return;
            
            var info = reference.Resolve();
            if (info.ResolveErrorType == ResolveErrorType.OK && info.DeclaredElement is IMethod stringMethod)
            {
                if (HasOverloadWithIntParameter(stringMethod, expression, out var argumentIndex, out var containingType))
                {
                    // extract argument for replace
                    var argument = expression.Arguments[argumentIndex];
                    var (clrName, methodName) = ourTypes[containingType.GetClrName()];
                    var literal = argument.Expression?.ConstantValue.Value as string;
                    if (literal == null) 
                        return;
 
                    consumer.AddHighlighting(new PreferAddressByIdToGraphicsParamsWarning(expression, argument, argument.Expression, literal, clrName.FullName, methodName));
                }
            }
        }

        private bool HasOverloadWithIntParameter(IMethod stringMethod, IInvocationExpression expression, out int index, out ITypeElement containingType)
        {
            index = stringMethod.Parameters.Count;
            containingType = null;
            var stringMethodName = stringMethod.ShortName;
            
            if (!stringMethodName.StartsWith("Get") && !stringMethodName.StartsWith("Set")) return false;
            
            containingType = stringMethod.GetContainingType();

            if (containingType == null || !ourTypes.ContainsKey(containingType.GetClrName()))
                return false;
            
            var type = TypeFactory.CreateType(containingType);
            var table = type.GetSymbolTable(expression.PsiModule).Filter(
                new AccessRightsFilter(new DefaultAccessContext(expression)),
                new ExactNameFilter(stringMethodName)
            );

            bool isFound = false;
            foreach (var symbol in table.GetSymbolInfos(stringMethodName))
            {
                if (!(symbol.GetDeclaredElement() is IMethod candidate))
                    continue;
                if (MatchSignatureStringToIntMethod(stringMethod, candidate, out index))
                {
                    if (isFound)
                    {
                        index = stringMethod.Parameters.Count;
                        return false;
                    }

                    isFound = true;
                } 
            } 

            return isFound;
        }

        private bool MatchSignatureStringToIntMethod([NotNull]IMethod stringMethod, [NotNull]IMethod intMethod, out int index)
        {
            index = stringMethod.Parameters.Count;
         
            // Heuristics:
            // try to find method with same parameters excluding one pair where string method has string parameter and
            // intMethod has int parameter. If several pairs found we will reject candidate

            if (stringMethod.TypeParameters.Count != intMethod.TypeParameters.Count)
                return false;
            
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

                    // Do not handle cases with strange pairs of name. 
                    if (isFound)
                    {
                        index = stringMethod.Parameters.Count;
                        return false;
                    }

                    isFound = true;
                    index = i;
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