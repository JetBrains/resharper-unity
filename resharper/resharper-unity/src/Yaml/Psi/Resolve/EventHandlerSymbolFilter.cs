using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class EventHandlerSymbolFilter : SimpleSymbolFilter
    {
        private IType myType;
        public EventHandlerSymbolFilter(int mode, string type, IPsiModule psiModule)
        {
            if (mode == 2)
            {
                myType = TypeFactory.CreateTypeByCLRName(type, psiModule);
            }
            else if (mode != 0)
            {
                var predefinedTypes = psiModule.GetPredefinedType();
                if (mode == 3)
                {
                    myType = predefinedTypes.Int;
                }
                else if (mode == 4)
                {
                    myType = predefinedTypes.Float;
                } 
                else if (mode == 5)
                {
                    myType = predefinedTypes.String;
                }
                else if (mode == 6)
                {
                    myType = predefinedTypes.Bool;
                }
            }
        }
        
        public override ResolveErrorType ErrorType => ResolveErrorType.ARGUMENTS_MISMATCH;
        
        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (!(declaredElement is IMethod method))
                return false;

            var parameters = method.Parameters;
            if (parameters.Count == 0 && myType == null)
                return true;

            if (parameters.Count == 1)
            {
                var param = parameters[0];
                return param.Type.Equals(myType);
            }

            return false;
        }
    }
}