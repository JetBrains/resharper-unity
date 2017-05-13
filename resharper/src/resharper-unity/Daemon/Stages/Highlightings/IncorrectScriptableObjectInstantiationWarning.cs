using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(IncorrectScriptableObjectInstantiationWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity,
    IncorrectScriptableObjectInstantiationWarning.MESSAGE,
    "Instantiating a ScriptableObject base class with 'new' means that Unity will not call any event functions. Use 'ScriptableObject.CreateInstance<T>()' instead.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING)]
    public class IncorrectScriptableObjectInstantiationWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.IncorrectScriptableObjectInstantiation";
        public const string MESSAGE = "ScriptableObjects must be instantiated with 'ScriptableObject.CreateInstance<T>()' instead of 'new'";

        public IncorrectScriptableObjectInstantiationWarning(IObjectCreationExpression creationExpression)
        {
            CreationExpression = creationExpression;
        }

        public IObjectCreationExpression CreationExpression { get; }

        public bool IsValid() => CreationExpression == null || CreationExpression.IsValid();
        public DocumentRange CalculateRange() => CreationExpression.GetHighlightingRange();
        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}