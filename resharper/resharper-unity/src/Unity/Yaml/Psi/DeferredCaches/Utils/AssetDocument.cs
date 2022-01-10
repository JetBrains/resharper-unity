using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils
{
    public class AssetDocument
    {
        public int StartOffset { get; }
        public IBuffer Buffer { get; }
        private readonly object myLockObject = new object();
        private IYamlDocument myDocument = null;
        public IYamlDocument Document
        {
            get
            {
                if (myDocument == null)
                {
                    lock (myLockObject)
                    {
                        if (myDocument != null)
                            return myDocument;
                        
                        var lexer = new YamlLexer(Buffer, true, false).ToCachingLexer();
                        var parser = new YamlParser(lexer);
                        myDocument = parser.ParseDocument();
                    }
                }

                return myDocument;
            }
        }

        public IHierarchyElement HierarchyElement { get; }

        public AssetDocument(int startOffset, IBuffer buffer, IHierarchyElement hierarchyElement)
        {
            StartOffset = startOffset;
            Buffer = buffer;
            HierarchyElement = hierarchyElement;
        }

        public AssetDocument WithHiererchyElement(IHierarchyElement hierarchyElement)
        {
            return new AssetDocument(StartOffset, Buffer, hierarchyElement);
        }
    }
}