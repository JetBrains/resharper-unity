using System.Text;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.UnityYaml.Psi.Parsing
{
    public class UnityYamlLexer : ILexer
    {
        private readonly IBuffer myBuffer;
        private readonly int myStartOffset;
        private readonly int myEndOffset;

        private int myCurOffset;
        private int myCurrentLineIndent;
        private bool myLineStart = true;

        private int myTokenStartOffset;
        private TokenNodeType myTokenNodeType;
        private YamlLexer myYamlLexer;


        public UnityYamlLexer(IBuffer buffer, int startOffset, int endOffset)
        {
            myBuffer = buffer;
            myStartOffset = startOffset;
            myEndOffset = endOffset;
            myYamlLexer = new YamlLexer(buffer, startOffset, endOffset);
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
                switch (myBuffer[myCurOffset])
                {
                    case '%':
                        while (true)
                        {
                            if (myCurOffset > myEndOffset)
                                break;

                            if (AdvanceChar())
                                break;
                        }

                        break;
                    case '-':
                        AdvanceChar();
                        // todo out of range
                        if (myCurOffset + 1 <= myEndOffset && myBuffer[myCurOffset] == '-' &&
                            myBuffer[myCurOffset + 1] == '-')
                        {
                            AdvanceChar();
                            AdvanceChar();
                            if (myCurOffset <= myEndOffset && myBuffer[myCurOffset] == ' ')
                            {
                                AdvanceChar();
                                var sb = new StringBuilder();
                                while (myCurOffset <= myEndOffset && myBuffer[myCurOffset] != ' '
                                                                  && myBuffer[myCurOffset] != '\r'
                                                                  && myBuffer[myCurOffset] != '\n')
                                {
                                    sb.Append(Buffer[myCurOffset]);
                                    myCurOffset++; // Yes, no AdvanceChar.. 
                                }

                                var tag = sb.ToString();
                                if (tag.Equals("!u!1") || tag.Equals("!u!4") || tag.Equals("!u!1001") ||
                                    tag.Equals("!u!114"))
                                {
                                    isInteresting = true;
                                }
                            }
                        }

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
                    if (myCurrentLineIndent == 0 && myCurOffset + 2 <= myEndOffset && (
                            (myBuffer[myCurOffset] == '-' && myBuffer[myCurOffset + 1] == '-' &&
                             myBuffer[myCurOffset + 2] == '-')))
                    {
#if DEBUG
                        myTokenNodeType = isInteresting ? UnityYamlTokenType.DOCUMENT : YamlTokenType.COMMENT;
#else
              myTokenNodeType = isInteresting ? YamlTokenType.DOCUMENT : YamlTokenType.USELESS_DOCUMENT;
#endif

                        break;
                    }

                    if (myCurOffset > myEndOffset)
                    {
                        myTokenNodeType = UnityYamlTokenType.DOCUMENT;
                        break;
                    }

                    AdvanceChar();
                }
            }
        }

        private bool AdvanceChar()
        {
            if (myCurOffset > myEndOffset)
                return false;

            if (myLineStart && myBuffer[myCurOffset] == ' ')
            {
                myCurrentLineIndent++;
            }

            if (myBuffer[myCurOffset] != ' ')
            {
                myLineStart = false;
            }

            if (myBuffer[myCurOffset] == '\n')
            {
                myLineStart = true;
                myCurrentLineIndent = 0;
                myCurOffset++;
                return true;
            }

            if (myBuffer[myCurOffset] == '\r')
            {
                myLineStart = true;
                myCurrentLineIndent = 0;
                myCurOffset++;
                if (myCurOffset <= myEndOffset && myBuffer[myCurOffset] == '\n')
                    myCurOffset++;
                return true;
            }

            myCurOffset++;
            return false;
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