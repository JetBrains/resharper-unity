using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class EventHandlerSymbolFilter : SimpleSymbolFilter
    {
        private readonly EventHandlerArgumentMode myMode;
        private readonly IType myType;
        public EventHandlerSymbolFilter(EventHandlerArgumentMode mode, string type, IPsiModule psiModule)
        {
            myMode = mode;
            if (mode == EventHandlerArgumentMode.UnityType && type != null)
            {
                myType = TypeFactory.CreateTypeByCLRName(type, psiModule);
            }
            else if (mode != EventHandlerArgumentMode.None)
            {
                var predefinedTypes = psiModule.GetPredefinedType();
                switch (mode)
                {
                    case EventHandlerArgumentMode.Int:
                        myType = predefinedTypes.Int;
                        break;
                    case EventHandlerArgumentMode.Float:
                        myType = predefinedTypes.Float;
                        break;
                    case EventHandlerArgumentMode.String:
                        myType = predefinedTypes.String;
                        break;
                    case EventHandlerArgumentMode.Bool:
                        myType = predefinedTypes.Bool;
                        break;
                }
            }
        }
        
        public override ResolveErrorType ErrorType => ResolveErrorType.ARGUMENTS_MISMATCH;
        
        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            if (myMode == EventHandlerArgumentMode.Unknown)
                return false;
            if (myMode != EventHandlerArgumentMode.None && myType == null)
                return false;
            
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