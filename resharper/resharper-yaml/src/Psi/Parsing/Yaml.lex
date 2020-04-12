using System;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

%%

%unicode

%init{
  currentTokenType = null;
  currentLineIndent = 0;
  flowLevel = 0;
%init}

%namespace JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing
%class YamlLexerGenerated
%implements IIncrementalLexer
%function _locateToken
%virtual
%public
%type TokenNodeType
%ignorecase

%eofval{
  currentTokenType = null; return currentTokenType;
%eofval}

%include Chars.lex

APOS_CHAR=\u0027
BACKSLASH_CHAR=\u005C
CARET_CHAR=\u005E
DOT_CHAR=\u002E
MINUS_CHAR=\u002D
LBRACK_CHAR=\u005B
RBRACK_CHAR=\u005D
LBRACE_CHAR=\u007B
RBRACE_CHAR=\u007D
QUOTE_CHAR=\u0022


YAML11_NEW_LINE_CHARS={LINE_SEPARATOR}{PARAGRAPH_SEPARATOR}
NEW_LINE_CHARS={CR}{LF}
NEW_LINE=({CR}?{LF}|{CR}|{LINE_SEPARATOR}|{PARAGRAPH_SEPARATOR})
NOT_NEW_LINE=([^{NEW_LINE_CHARS}{YAML11_NEW_LINE_CHARS}])

INPUT_CHARACTER={NOT_NEW_LINE}

WHITESPACE_CHARS={SP}{TAB}
WHITESPACE=[{WHITESPACE_CHARS}]+
OPTIONAL_WHITESPACE=[{WHITESPACE_CHARS}]*

ASCII_COMMON=[0-9a-zA-Z]
ASCII_SYMBOLS=[\u0021-\u002F\u003A-\u0040\u005B-\u0060\u007B-\u007E]
ASCII_PRINTABLE={ASCII_COMMON}|{ASCII_SYMBOLS}
OTHER_PRINTABLE={NEL}|[\u00A0-\uD7FF]|[\uE000-\uFFFD]|[\u10000-\10FFFF]

C_PRINTABLE={TAB}|{SP}|{CR}|{LF}|{ASCII_PRINTABLE}|{OTHER_PRINTABLE}
C_INDICATOR=[{MINUS_CHAR}?:,{LBRACK_CHAR}{RBRACK_CHAR}{LBRACE_CHAR}{RBRACE_CHAR}#&*!|>'{QUOTE_CHAR}%@`]
C_FLOW_INDICATOR=[,{LBRACK_CHAR}{RBRACK_CHAR}{LBRACE_CHAR}{RBRACE_CHAR}]

ASCII_SYMBOLS__MINUS_C_INDICATOR=[$()+{DOT_CHAR}/;<={BACKSLASH_CHAR}{CARET_CHAR}_~]
ASCII_SYMBOLS__MINUS_C_FLOW_INDICATOR=[!{QUOTE_CHAR}#$%&'()*+{MINUS_CHAR}{DOT_CHAR}/:;<=>?@{BACKSLASH_CHAR}{CARET_CHAR}_`|~]

NB_JSON=({TAB}|[\u0020-\u10FFFF])
NB_CHAR=({TAB}|{SP}|{ASCII_PRINTABLE}|{OTHER_PRINTABLE})
NS_CHAR=({ASCII_PRINTABLE}|{OTHER_PRINTABLE})

NB_JSON__MINUS_SINGLE_QUOTE=({TAB}|[\u0020-\u0026\u0028-\u10FFFF])
NB_JSON__MINUS_DOUBLE_QUOTE=({TAB}|[\u0020-\u0021\u0023-\u10FFFF])
NS_CHAR__MINUS_C_INDICATOR=({ASCII_COMMON}|{ASCII_SYMBOLS__MINUS_C_INDICATOR}|{OTHER_PRINTABLE})
NS_CHAR__MINUS_C_FLOW_INDICATOR=({ASCII_COMMON}|{ASCII_SYMBOLS__MINUS_C_FLOW_INDICATOR}|{OTHER_PRINTABLE})

URI_SYMBOLS=[#;/?:@&=+$,_{DOT_CHAR}!~*'(){LBRACK_CHAR}{RBRACK_CHAR}]
URI_SYMBOLS__MINUS_BANG_AND_C_FLOW_INDICATOR=[#;/?:@&=+$_{DOT_CHAR}~*'()]

NS_DEC_DIGIT=[0-9]
NS_HEX_DIGIT=[0-9a-fA-F]
NS_ASCII_LETTER=[a-zA-Z]

NS_WORD_CHAR=({NS_DEC_DIGIT}|{NS_ASCII_LETTER}|"-")

URL_ENCODED_CHAR=("%"{NS_HEX_DIGIT}{NS_HEX_DIGIT})
NS_URI_CHAR=({URL_ENCODED_CHAR}|{NS_WORD_CHAR}|{URI_SYMBOLS})
NS_TAG_CHAR=({URL_ENCODED_CHAR}|{NS_WORD_CHAR}|{URI_SYMBOLS__MINUS_BANG_AND_C_FLOW_INDICATOR})

NS_PLAIN_SAFE_IN={NS_CHAR__MINUS_C_FLOW_INDICATOR}
NS_PLAIN_SAFE_OUT={NS_CHAR}


NS_PLAIN_SAFE_OUT__MINUS_COLON_AND_HASH=({ASCII_COMMON}|{OTHER_PRINTABLE}|[\u0021\u0022\u0024-\u002F\u003B-\u0040\u005B-\u0060\u007B-\u007E])
NS_PLAIN_SAFE_IN__MINUS_COLON_AND_HASH=({ASCII_COMMON}|{OTHER_PRINTABLE}|[!{QUOTE_CHAR}$%&'()*+{MINUS_CHAR}{DOT_CHAR}/;<=>?@{BACKSLASH_CHAR}{CARET_CHAR}_`|~])

NS_PLAIN_CHAR__IN=({NS_PLAIN_SAFE_IN__MINUS_COLON_AND_HASH}|({NS_CHAR}"#")|(":"{NS_PLAIN_SAFE_IN}))
NS_PLAIN_FIRST__IN=({NS_CHAR__MINUS_C_INDICATOR}|([?:-]{NS_PLAIN_SAFE_IN}))
NB_NS_PLAIN_IN_LINE__IN=({OPTIONAL_WHITESPACE}{NS_PLAIN_CHAR__IN})*
NS_PLAIN_ONE_LINE__IN={NS_PLAIN_FIRST__IN}{NB_NS_PLAIN_IN_LINE__IN}


NS_PLAIN_CHAR__OUT=({NS_PLAIN_SAFE_OUT__MINUS_COLON_AND_HASH}|({NS_CHAR}"#")|(":"{NS_PLAIN_SAFE_OUT}))
NS_PLAIN_FIRST__OUT=({NS_CHAR__MINUS_C_INDICATOR}|([?:-]{NS_PLAIN_SAFE_OUT}))
NB_NS_PLAIN_IN_LINE__OUT=({OPTIONAL_WHITESPACE}{NS_PLAIN_CHAR__OUT})*
NS_PLAIN_ONE_LINE__OUT={NS_PLAIN_FIRST__OUT}{NB_NS_PLAIN_IN_LINE__OUT}


B_BREAK={NEW_LINE}
B_NON_CONTENT={B_BREAK}
B_AS_SPACE={B_BREAK}
B_AS_LINE_FEED={B_BREAK}
L_EMPTY=({WHITESPACE}{B_AS_LINE_FEED})
B_L_TRIMMED={B_NON_CONTENT}{L_EMPTY}+
B_L_FOLDED={B_L_TRIMMED}|{B_AS_SPACE}

S_FLOW_LINE_PREFIX={WHITESPACE}

S_FLOW_FOLDED={OPTIONAL_WHITESPACE}{B_L_FOLDED}{S_FLOW_LINE_PREFIX}

NS_PLAIN_NEXT_LINE__IN={NS_PLAIN_CHAR__IN}{NB_NS_PLAIN_IN_LINE__IN}
S_NS_PLAIN_NEXT_LINE__IN=({S_FLOW_FOLDED}{NS_PLAIN_NEXT_LINE__IN})
NS_PLAIN_MULTI_LINE__IN={NS_PLAIN_ONE_LINE__IN}{S_NS_PLAIN_NEXT_LINE__IN}*

NS_PLAIN_NEXT_LINE__OUT={NS_PLAIN_CHAR__OUT}{NB_NS_PLAIN_IN_LINE__OUT}
S_NS_PLAIN_NEXT_LINE__OUT=({S_FLOW_FOLDED}{NS_PLAIN_NEXT_LINE__OUT})
NS_PLAIN_MULTI_LINE__OUT={NS_PLAIN_ONE_LINE__OUT}{S_NS_PLAIN_NEXT_LINE__OUT}*


IMPLICIT_BLOCK_KEY={NS_PLAIN_ONE_LINE__OUT}{OPTIONAL_WHITESPACE}":"
IMPLICIT_FLOW_KEY={NS_PLAIN_ONE_LINE__IN}{OPTIONAL_WHITESPACE}":"


NS_ANCHOR_CHAR=({NS_CHAR__MINUS_C_FLOW_INDICATOR})
NS_ANCHOR_NAME={NS_ANCHOR_CHAR}+

C_QUOTED_QUOTE=({APOS_CHAR}{APOS_CHAR})
NB_SINGLE_CHAR=({C_QUOTED_QUOTE}|{NB_JSON__MINUS_SINGLE_QUOTE})
NB_SINGLE_ONE_LINE={NB_SINGLE_CHAR}*
NB_SINGLE_MULTI_LINE=({C_QUOTED_QUOTE}|{NB_JSON__MINUS_SINGLE_QUOTE}|{NEW_LINE})*
C_SINGLE_QUOTED_key={APOS_CHAR}{NB_SINGLE_ONE_LINE}{APOS_CHAR}
C_SINGLE_QUOTED_flow={APOS_CHAR}{NB_SINGLE_MULTI_LINE}{APOS_CHAR}

NB_DOUBLE_CHAR=({NB_JSON__MINUS_DOUBLE_QUOTE}|{BACKSLASH_CHAR}{QUOTE_CHAR})
NB_DOUBLE_ONE_LINE={NB_DOUBLE_CHAR}*
NB_DOUBLE_MULTI_LINE=({NB_JSON__MINUS_DOUBLE_QUOTE}|{BACKSLASH_CHAR}{QUOTE_CHAR}|{NEW_LINE})*
C_DOUBLE_QUOTED_key={QUOTE_CHAR}{NB_DOUBLE_ONE_LINE}{QUOTE_CHAR}
C_DOUBLE_QUOTED_flow={QUOTE_CHAR}{NB_DOUBLE_MULTI_LINE}{QUOTE_CHAR}

C_NB_COMMENT_TEXT="#"{NB_CHAR}*

C_DIRECTIVES_END=^"---"
C_DOCUMENT_END=^"..."

MAP_VALUE_START = (.|{NEW_LINE}) 


%state BLOCK, FLOW, MAP_VALUE
%state DIRECTIVE
%state BLOCK_SCALAR_HEADER, BLOCK_SCALAR
%state JSON_ADJACENT_VALUE
%state ANCHOR_ALIAS
%state SHORTHAND_TAG, VERBATIM_TAG

%%
<MAP_VALUE>     {MAP_VALUE_START}         { return TryEatLinesWithGreaterIndent();}

<YYINITIAL, BLOCK, FLOW>
                ^{WHITESPACE}           { if (!AtChameleonStart) { currentLineIndent = yylength(); return YamlTokenType.INDENT; } else { AtChameleonStart = false; return YamlTokenType.WHITESPACE;}  }
<YYINITIAL, BLOCK, FLOW>
                {WHITESPACE}            { return YamlTokenType.WHITESPACE; }
<YYINITIAL, BLOCK, FLOW>
                {NEW_LINE}              { HandleNewLine(); return YamlTokenType.NEW_LINE; }

<YYINITIAL>     ^"%"                    { yybegin(DIRECTIVE); return YamlTokenType.PERCENT; }
<YYINITIAL>     {C_DIRECTIVES_END}      { currentLineIndent = 0; yybegin(BLOCK); return YamlTokenType.DIRECTIVES_END; }
<YYINITIAL>     .                       { currentLineIndent = 0; yybegin(BLOCK); RewindToken(); return _locateToken(); }

<YYINITIAL, BLOCK, BLOCK_SCALAR>
                {C_DOCUMENT_END}        { yybegin(YYINITIAL); return YamlTokenType.DOCUMENT_END; }


<BLOCK>         {C_DIRECTIVES_END}      { return YamlTokenType.DIRECTIVES_END; }

<BLOCK>         {NS_PLAIN_ONE_LINE__OUT}  { return YamlTokenType.NS_PLAIN_ONE_LINE_OUT; }
<BLOCK>         {IMPLICIT_BLOCK_KEY}      { return HandleImplicitKey(); }


<BLOCK, FLOW>   "@"                     { return YamlTokenType.AT; }
<BLOCK, FLOW>   "`"                     { return YamlTokenType.BACKTICK; }
<BLOCK, FLOW>   "&"                     { yybegin(ANCHOR_ALIAS); return YamlTokenType.AMP; }
<BLOCK, FLOW>   "*"                     { yybegin(ANCHOR_ALIAS); return YamlTokenType.ASTERISK; }
<BLOCK, FLOW>   "!"                     { yybegin(SHORTHAND_TAG); return YamlTokenType.BANG; }
<BLOCK, FLOW>   "!<"                    { yybegin(VERBATIM_TAG); return YamlTokenType.BANG_LT; }
<BLOCK, FLOW>   ">"                     { BeginBlockScalar(); return YamlTokenType.GT; }
<BLOCK, FLOW>   "|"                     { BeginBlockScalar(); return YamlTokenType.PIPE; }
<BLOCK>         ":"                     { if (explicitKey) {explicitKey = false;} else if (myAllowChameleonOptimizations && flowLevel == 0 && currentLineIndent > 0) {yybegin(MAP_VALUE);} return YamlTokenType.COLON; }
<FLOW>          ":"                     { return YamlTokenType.COLON; }
<BLOCK, FLOW>   ","                     { return YamlTokenType.COMMA; }
<FLOW>          "-"                     { HandleSequenceItemIndicator(); return YamlTokenType.MINUS; }
<BLOCK>         "-"                     { HandleIndent(); HandleSequenceItemIndicator(); return YamlTokenType.MINUS; }
<BLOCK, FLOW>   "<"                     { return YamlTokenType.LT; }
<BLOCK, FLOW>   "{"                     { PushFlowIndicator(); return YamlTokenType.LBRACE; }
<BLOCK, FLOW>   "}"                     { PopFlowIndicator(); BeginJsonAdjacentValue(); return YamlTokenType.RBRACE; }
<BLOCK, FLOW>   "["                     { PushFlowIndicator(); return YamlTokenType.LBRACK; }
<BLOCK, FLOW>   "]"                     { PopFlowIndicator(); BeginJsonAdjacentValue(); return YamlTokenType.RBRACK; }
<BLOCK, FLOW>   "%"                     { return YamlTokenType.PERCENT; }
<BLOCK, FLOW>   "?"                     { HandleExplicitKeyIndicator(); return YamlTokenType.QUESTION; }

<YYINITIAL, DIRECTIVE, BLOCK_SCALAR_HEADER, BLOCK>
                {C_NB_COMMENT_TEXT}     { return YamlTokenType.COMMENT; }

<BLOCK, FLOW>   {C_SINGLE_QUOTED_key}   { BeginJsonAdjacentValue(); return YamlTokenType.C_SINGLE_QUOTED_SINGLE_LINE; }
<BLOCK, FLOW>   {C_SINGLE_QUOTED_flow}  { BeginJsonAdjacentValue(); return YamlTokenType.C_SINGLE_QUOTED_MULTI_LINE; }
<BLOCK, FLOW>   {C_DOUBLE_QUOTED_key}   { BeginJsonAdjacentValue(); return YamlTokenType.C_DOUBLE_QUOTED_SINGLE_LINE; }
<BLOCK, FLOW>   {C_DOUBLE_QUOTED_flow}  { BeginJsonAdjacentValue(); return YamlTokenType.C_DOUBLE_QUOTED_MULTI_LINE; }


<JSON_ADJACENT_VALUE> ":"               { EndJsonAdjacentValue(); return YamlTokenType.COLON; }
<JSON_ADJACENT_VALUE> {NEW_LINE}        { return AbandonJsonAdjacentValueState(); }
<JSON_ADJACENT_VALUE> .                 { return AbandonJsonAdjacentValueState(); }


<FLOW>          {NS_PLAIN_ONE_LINE__IN} { return YamlTokenType.NS_PLAIN_ONE_LINE_IN; }
<FLOW>          {IMPLICIT_FLOW_KEY}     { return HandleImplicitKey(); }


<DIRECTIVE>     {WHITESPACE}            { return YamlTokenType.WHITESPACE; }
<DIRECTIVE>     {NEW_LINE}              { HandleNewLine(); yybegin(YYINITIAL); return YamlTokenType.NEW_LINE; }
<DIRECTIVE>     {NS_CHAR}+              { return YamlTokenType.NS_CHARS; }
<DIRECTIVE>     {NS_WORD_CHAR}+         { return YamlTokenType.NS_WORD_CHARS; }
<DIRECTIVE>     {NS_URI_CHAR}+          { return YamlTokenType.NS_URI_CHARS; }


<BLOCK_SCALAR_HEADER>
                {NEW_LINE}              { HandleNewLine(false); yybegin(BLOCK_SCALAR); return YamlTokenType.NEW_LINE; }
<BLOCK_SCALAR_HEADER>
                "+"                     { return YamlTokenType.PLUS; }
<BLOCK_SCALAR_HEADER>
                "-"                     { return YamlTokenType.MINUS; }
<BLOCK_SCALAR_HEADER>
                {NS_DEC_DIGIT}          { return YamlTokenType.NS_DEC_DIGIT; }
<BLOCK_SCALAR_HEADER>
                {WHITESPACE}            { return YamlTokenType.WHITESPACE; }

<BLOCK_SCALAR>  {NEW_LINE}              { HandleNewLine(false); return YamlTokenType.NEW_LINE; }
<BLOCK_SCALAR>  ^{WHITESPACE}           { currentLineIndent = yylength(); HandleBlockScalarWhitespace(); return YamlTokenType.INDENT; }
<BLOCK_SCALAR>  {WHITESPACE}            { HandleBlockScalarWhitespace(); return YamlTokenType.WHITESPACE; }
<BLOCK_SCALAR>  {NB_CHAR}+              { return YamlTokenType.SCALAR_TEXT; }
<BLOCK_SCALAR>  ^([^{WHITESPACE_CHARS}{NEW_LINE_CHARS}]){NB_CHAR}+
                                        { return HandleBlockScalarLine(); }


<ANCHOR_ALIAS>  {WHITESPACE}            { ResetBlockFlowState(); return YamlTokenType.WHITESPACE; }
<ANCHOR_ALIAS>  {NEW_LINE}              { HandleNewLine(); ResetBlockFlowState(); return YamlTokenType.NEW_LINE; }
<ANCHOR_ALIAS>  {NS_ANCHOR_NAME}        { ResetBlockFlowState(); return YamlTokenType.NS_ANCHOR_NAME; }


<SHORTHAND_TAG, VERBATIM_TAG>
                {WHITESPACE}            { ResetBlockFlowState(); return YamlTokenType.WHITESPACE; }
<SHORTHAND_TAG, VERBATIM_TAG>
                {NEW_LINE}              { HandleNewLine(); ResetBlockFlowState(); return YamlTokenType.NEW_LINE; }

<SHORTHAND_TAG> "!"                     { return YamlTokenType.BANG; }
<SHORTHAND_TAG> {NS_TAG_CHAR}+          { ResetBlockFlowState(); return YamlTokenType.NS_TAG_CHARS; }


<VERBATIM_TAG>  {NS_URI_CHAR}+          { return YamlTokenType.NS_URI_CHARS; }
<VERBATIM_TAG>  ">"                     { ResetBlockFlowState(); return YamlTokenType.GT; }


<DIRECTIVE, BLOCK, FLOW, BLOCK_SCALAR_HEADER, BLOCK_SCALAR, ANCHOR_ALIAS, SHORTHAND_TAG, VERBATIM_TAG>
                .                       { return YamlTokenType.BAD_CHARACTER; }