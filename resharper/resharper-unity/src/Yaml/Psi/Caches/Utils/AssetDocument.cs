using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.Utils
{
    public class AssetDocument
    {
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

        public AssetDocument(IBuffer buffer)
        {
            Buffer = buffer;
        }
    }
}