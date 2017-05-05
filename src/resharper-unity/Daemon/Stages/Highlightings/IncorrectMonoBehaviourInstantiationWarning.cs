using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(IncorrectMonoBehaviourInstantiationWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity,
    IncorrectMonoBehaviourInstantiationWarning.MESSAGE,
    "Instantiating a MonoBehaviour based class with 'new' does not attach it to a GameObject, and Unity will not invoke any event functions. Create a new instance using 'GameObject.AddComponent<T>()",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING)]
    public class IncorrectMonoBehaviourInstantiationWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.IncorrectMonoBehaviourInstantiation";
        public const string MESSAGE = "MonoBehaviours must be instantiated with 'GameObject.AddComponent<T>()' instead of 'new'";

        public IncorrectMonoBehaviourInstantiationWarning(IObjectCreationExpression creationExpression)
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