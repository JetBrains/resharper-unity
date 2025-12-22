using System;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Psi.Tree
{
    public abstract class JsonNewCompositeNodeType : CompositeNodeType
    {
        protected JsonNewCompositeNodeType(string s, int index, Type nodeType)
            : base(s, index, nodeType)
        {
        }
    }
}