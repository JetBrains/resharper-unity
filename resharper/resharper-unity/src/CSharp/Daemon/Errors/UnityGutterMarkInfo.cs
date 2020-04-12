using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors
{
    public partial class UnityGutterMarkInfo : ICustomAttributeIdHighlighting
    {
        public IEnumerable<BulbMenuItem> Actions { get; }

        public UnityGutterMarkInfo(IEnumerable<BulbMenuItem> actions, IDeclaration declaration, string text) : this(declaration, text)
        {
            Actions = actions;
        }
        
        public string AttributeId => UnityHighlightingAttributeIds.UNITY_GUTTER_ICON_ATTRIBUTE;
    }
    
    public partial class UnityHotGutterMarkInfo : ICustomAttributeIdHighlighting
    {
        public IEnumerable<BulbMenuItem> Actions { get; }

        public UnityHotGutterMarkInfo(IEnumerable<BulbMenuItem> actions, IDeclaration declaration, string text) : this(declaration, text)
        {
            Actions = actions;
        }
        
        public string AttributeId => UnityHighlightingAttributeIds.UNITY_PERFORMANCE_CRITICAL_GUTTER_ICON_ATTRIBUTE;
    }
}