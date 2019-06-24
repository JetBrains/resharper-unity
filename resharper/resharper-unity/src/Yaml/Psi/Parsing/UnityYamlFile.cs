using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{
    public class UnityYamlFile : FileElementBase, IYamlFile
    {
        private CachedPsiValue<Dictionary<int, IYamlDocument>> myData =
            new CachedPsiValue<Dictionary<int, IYamlDocument>>();

        IYamlDocument GetDocument(int i)
        {
            myData.GetValue(this, (d) => new Dictionary<int, IYamlDocument>());
            throw new NotImplementedException();
        }


        public override PsiLanguageType Language => UnityYamlLanguage.Instance;

        public const short DOCUMENT = ChildRole.LAST + 1;

        internal UnityYamlFile()
            : base()
        {
        }

        public override NodeType NodeType => ElementType.YAML_FILE;

        public void Accept(TreeNodeVisitor visitor) =>
            visitor.VisitYamlFileNode(this);

        public void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context) =>
            visitor.VisitYamlFileNode(this, context);

        public TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context) =>
            visitor.VisitYamlFileNode(this, context);

        public override short GetChildRole(TreeElement child)
        {
            switch (child.NodeType.Index)
            {
                case ElementType.YAML_DOCUMENT_NODE_TYPE_INDEX:
                    return DOCUMENT;
            }

            return 0;
        }

        public virtual JetBrains.ReSharper.Psi.Tree.TreeNodeCollection<IYamlDocument> Documents =>
            FindListOfChildrenByRole<IYamlDocument>(DOCUMENT);

        public virtual JetBrains.ReSharper.Psi.Tree.TreeNodeEnumerable<IYamlDocument> DocumentsEnumerable =>
            AsChildrenEnumerable<IYamlDocument>(DOCUMENT);

        public override string ToString() => "IYamlFile";
    }
}