using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing
{
    public class UnityYamlLexer : ILexer
    {
        private readonly IBuffer myBuffer;
        private readonly int myStartOffset;
        private readonly int myEndOffset;

        private int myCurOffset;
        private int myCurrentLineOffset;

        private int myTokenStartOffset;
        private TokenNodeType myTokenNodeType;


        public UnityYamlLexer(IBuffer buffer, int startOffset, int endOffset)
        {
            myBuffer = buffer;
            myStartOffset = startOffset;
            myEndOffset = endOffset;
        }

        public void Start()
        {
            myCurOffset = myStartOffset;
            Advance();
        }

        public void Advance()
        {
            if (myCurOffset > myEndOffset)
            {
                myTokenNodeType = null;
                return;
            }

            var isInteresting = false;
            myTokenStartOffset = myCurOffset;
            while (true)
            {
                if (myCurOffset > myEndOffset)
                {
                    EatUntilDocumentEnd();
                    return;
                }
                
                switch (myBuffer[myCurOffset])
                {
                    case '%':
                        while (true)
                        {
                            if (myCurOffset > myEndOffset)
                            {
                                myTokenNodeType = UnityYamlTokenType.DOCUMENT;
                                return;
                            }

                            if (myBuffer[myCurOffset] == '\r')
                            {
                                myCurOffset++;
                                myCurrentLineOffset = 0;
                                if (myCurOffset <= myEndOffset && myBuffer[myCurOffset] == '\n')
                                    myCurOffset++;
                                break;
                            }

                            if (myBuffer[myCurOffset] == '\n')
                            {
                                myCurOffset++;
                                myCurrentLineOffset = 0;
                                break;
                            }
                            myCurOffset++;
                        }

                        break;
                    case '-':
                        myCurOffset++;
                        if (myCurOffset + 1 <= myEndOffset && myBuffer[myCurOffset] == '-' &&
                            myBuffer[myCurOffset + 1] == '-')
                        {
                            myCurOffset++;
                            myCurOffset++;
                            if (myCurOffset <= myEndOffset && myBuffer[myCurOffset] == ' ')
                            {
                                myCurOffset++;
                                var sb = new StringBuilder();
                                while (myCurOffset <= myEndOffset && myBuffer[myCurOffset] != ' '
                                                                  && myBuffer[myCurOffset] != '\r'
                                                                  && myBuffer[myCurOffset] != '\n')
                                {
                                    sb.Append(Buffer[myCurOffset]);
                                    myCurOffset++; 
                                }

                                var tag = sb.ToString();
                                if (tag.Equals("!u!1") || tag.Equals("!u!4") || tag.Equals("!u!1001") ||
                                    tag.Equals("!u!114") || tag.Equals("!u!224"))
                                {
                                    isInteresting = true;
                                }
                            }
                        }

                        myCurrentLineOffset = 1; // just mark that it is not line start
                        EatUntilDocumentEnd();
                        return;

                    default:
                        EatUntilDocumentEnd();
                        return;
                }
            }

            void EatUntilDocumentEnd()
            {
                while (true)
                {
                    if (myCurOffset > myEndOffset)
                    {
                        myTokenNodeType = UnityYamlTokenType.DOCUMENT;
                        break;
                    }

                    var curChar = myBuffer[myCurOffset];
                    if (curChar == '\r')
                    {
                        myCurrentLineOffset = 0;
                        myCurOffset++;
                        continue;
                    }

                    if (curChar == '\n')
                    {
                        myCurrentLineOffset = 0;
                        myCurOffset++;
                        continue;
                    }
                    
                    if (myCurrentLineOffset == 0 && myCurOffset + 2 <= myEndOffset && (
                            (curChar == '-' && myBuffer[myCurOffset + 1] == '-' &&
                             myBuffer[myCurOffset + 2] == '-')))
                    {
                        
                        myTokenNodeType = isInteresting ? UnityYamlTokenType.DOCUMENT : UnityYamlTokenType.USELESS_DOCUMENT;
                        // debug highlightings...
                        // myTokenNodeType = isInteresting ? UnityYamlTokenType.DOCUMENT : YamlTokenType.COMMENT;

                        break;
                    }

                    myCurrentLineOffset++;
                    myCurOffset++;

                }
            }
        }

        public object CurrentPosition
        {
            get => myCurOffset;
            set => myCurOffset = (int) value;
        }

        public TokenNodeType TokenType => myTokenNodeType;

        public int TokenStart => myTokenStartOffset;

        public int TokenEnd => myCurOffset;

        public IBuffer Buffer => myBuffer;
    }
}