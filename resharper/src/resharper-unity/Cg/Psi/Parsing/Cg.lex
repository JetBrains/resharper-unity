using System;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

%%

%unicode

%init{
  currentTokenType = null;
%init}

%namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing
%class CgLexerGenerated
%implements IIncrementalLexer
%function _locateToken
%virtual
%public
%type TokenNodeType
%ignorecase

%eofval{
  currentTokenType = null; return currentTokenType;
%eofval}

%{
  /* Offsets error messages by 61 lines */
%}
%include Chars.lex

%{ /* TODO: Does Unity support crazy Unicode chars? */ 
%}
NEW_LINE_CHARS={CR}{LF}{NEL}{UNICODE_ZL_CHARS}{UNICODE_ZP_CHARS}
NEW_LINE=({CR}?{LF}|{CR}|{NEL}|{UNICODE_ZL}|{UNICODE_ZP})
NOT_NEW_LINE=([^{NEW_LINE_CHARS}])
LINE_CONTINUATOR=(\\([\ \t]*)({NEW_LINE}))

INPUT_CHARACTER={NOT_NEW_LINE}

WHITESPACE_CHARS={UNICODE_ZS_CHARS}{UNICODE_CC_WHITESPACE_CHARS}{UNICODE_CF_WHITESPACE_CHARS}{NULL_CHAR}
VERTICAL_WHITESPACE_CHARS={UNICODE_CC_SEPARATOR_CHARS}{UNICODE_ZL_CHARS}{UNICODE_ZP_CHARS}
WHITESPACE_OPTIONAL=([{WHITESPACE_CHARS}])*
WHITESPACE=([{WHITESPACE_CHARS}])+

SLASH="/"
ASTERISK="*"
NOT_ASTERISK=[^{ASTERISK}]
ASTERISKS={ASTERISK}+

DELIMITED_COMMENT_SECTION=([^\*]|({ASTERISKS}[^\*\/]))
UNFINISHED_DELIMITED_COMMENT="/*"{DELIMITED_COMMENT_SECTION}*
DELIMITED_COMMENT={UNFINISHED_DELIMITED_COMMENT}{ASTERISKS}"/"
SINGLE_LINE_COMMENT=("//"{INPUT_CHARACTER}*)
DELIMITED_COMMENT_POSTFIX = ({ASTERISKS}"/")

MINUS="-"
PLUS="+"
DECIMAL_DIGIT=[0-9]
NUMERIC_LITERAL=({MINUS}|{PLUS})?{DECIMAL_DIGIT}+(\.{DECIMAL_DIGIT}+f?)?

UNDERSCORE="_"
LETTER_CHAR={UNICODE_LL}|{UNICODE_LM}|{UNICODE_LO}|{UNICODE_LT}|{UNICODE_LU}|{UNICODE_NL}
IDENTIFIER_START_CHAR=({UNDERSCORE}|{LETTER_CHAR})
IDENTIFIER_PART_CHAR=({IDENTIFIER_START_CHAR}|{DECIMAL_DIGIT})
IDENTIFIER=({IDENTIFIER_START_CHAR}{IDENTIFIER_PART_CHAR}*)

NON_EOL_WS=(([\ \t])|({LINE_CONTINUATOR}))
PRP=("#"({NON_EOL_WS}|{DELIMITED_COMMENT}|{UNFINISHED_DELIMITED_COMMENT})*)

DIRECTIVE=({PRP}{IDENTIFIER})

%{ /*no empty directive support*/
%}

SLASH_AND_NOT_SLASH=("/"[^\/\u0085\u2028\u2029\u000D\u000A])
NOT_SLASH_NOT_NEW_LINE=([^\/\u0085\u2028\u2029\u000D\u000A])
DIRECTIVE_CONTENT=(({LINE_CONTINUATOR}|{DELIMITED_COMMENT}|{SLASH_AND_NOT_SLASH}|{NOT_SLASH_NOT_NEW_LINE})*)(\/|{SINGLE_LINE_COMMENT})?

%state YY_DIRECTIVE

%%

<YYINITIAL>           {WHITESPACE}            { return CgTokenNodeTypes.WHITESPACE; }
<YYINITIAL>           {NEW_LINE}              { return CgTokenNodeTypes.NEW_LINE; }

<YY_DIRECTIVE>        {DIRECTIVE_CONTENT}     { yybegin(YYINITIAL);         return CgTokenNodeTypes.DIRECTIVE_CONTENT;     }
<YYINITIAL>           {DIRECTIVE}             { yybegin(YY_DIRECTIVE);      return CgTokenNodeTypes.DIRECTIVE;             }

<YYINITIAL>           "{"                     { return CgTokenNodeTypes.LBRACE; }
<YYINITIAL>           "}"                     { return CgTokenNodeTypes.RBRACE; }
<YYINITIAL>           "("                     { return CgTokenNodeTypes.LPAREN; }
<YYINITIAL>           ")"                     { return CgTokenNodeTypes.RPAREN; }
<YYINITIAL>           "["                     { return CgTokenNodeTypes.LBRACKET; }
<YYINITIAL>           "]"                     { return CgTokenNodeTypes.RBRACKET; }
<YYINITIAL>           "."                     { return CgTokenNodeTypes.DOT; }
<YYINITIAL>           ","                     { return CgTokenNodeTypes.COMMA; }
<YYINITIAL>           ";"                     { return CgTokenNodeTypes.SEMICOLON; }
<YYINITIAL>           ":"                     { return CgTokenNodeTypes.COLON; }

<YYINITIAL>           "<"                     { return CgTokenNodeTypes.LT; }
<YYINITIAL>           ">"                     { return CgTokenNodeTypes.GT; }
<YYINITIAL>           "<="                    { return CgTokenNodeTypes.LTEQ; }
<YYINITIAL>           ">="                    { return CgTokenNodeTypes.GTEQ; }
<YYINITIAL>           "=="                    { return CgTokenNodeTypes.EQEQ; }
<YYINITIAL>           "!="                    { return CgTokenNodeTypes.NOTEQ; }

<YYINITIAL>           "="                     { return CgTokenNodeTypes.EQUALS; }

<YYINITIAL>           "+"                     { return CgTokenNodeTypes.PLUS; }
<YYINITIAL>           "-"                     { return CgTokenNodeTypes.MINUS; }
<YYINITIAL>           "*"                     { return CgTokenNodeTypes.MULTIPLY; }
<YYINITIAL>           "/"                     { return CgTokenNodeTypes.DIVIDE; }
<YYINITIAL>           "%"                     { return CgTokenNodeTypes.MODULO; }

<YYINITIAL>           "*="                    { return CgTokenNodeTypes.MULTEQ; }
<YYINITIAL>           "/="                    { return CgTokenNodeTypes.DIVEQ; }
<YYINITIAL>           "%="                    { return CgTokenNodeTypes.PERCEQ; }
<YYINITIAL>           "+="                    { return CgTokenNodeTypes.PLUSEQ; }
<YYINITIAL>           "-="                    { return CgTokenNodeTypes.MINUSEQ; }

<YYINITIAL>           "~"                     { return CgTokenNodeTypes.TILDE; }
<YYINITIAL>           "<<"                    { return CgTokenNodeTypes.LTLT; }
<YYINITIAL>           ">>"                    { return CgTokenNodeTypes.GTGT; }
<YYINITIAL>           "^"                     { return CgTokenNodeTypes.XOR; }
<YYINITIAL>           "|"                     { return CgTokenNodeTypes.OR; }
<YYINITIAL>           "&&"                    { return CgTokenNodeTypes.ANDAND; }
<YYINITIAL>           "||"                    { return CgTokenNodeTypes.OROR; }
<YYINITIAL>           "!"                     { return CgTokenNodeTypes.NEGATE; }

<YYINITIAL>           "<<="                   { return CgTokenNodeTypes.LTLTEQ; }
<YYINITIAL>           ">>="                   { return CgTokenNodeTypes.GTGTEQ; }
<YYINITIAL>           "&="                    { return CgTokenNodeTypes.ANDEQ; }
<YYINITIAL>           "^="                    { return CgTokenNodeTypes.XOREQ; }
<YYINITIAL>           "|="                    { return CgTokenNodeTypes.OREQ; }

<YYINITIAL>           "++"                    { return CgTokenNodeTypes.PLUSPLUS; }
<YYINITIAL>           "--"                    { return CgTokenNodeTypes.MINUSMINUS; }

<YYINITIAL>           {SINGLE_LINE_COMMENT}              { return CgTokenNodeTypes.SINGLE_LINE_COMMENT; }
<YYINITIAL>           {DELIMITED_COMMENT}                { return CgTokenNodeTypes.DELIMITED_COMMENT; }
<YYINITIAL>           {UNFINISHED_DELIMITED_COMMENT}     { return CgTokenNodeTypes.UNFINISHED_DELIMITED_COMMENT; }

<YYINITIAL>           {IDENTIFIER}            { return FindKeywordByCurrentToken() ?? CgTokenNodeTypes.IDENTIFIER; }
<YYINITIAL>           {NUMERIC_LITERAL}       { return CgTokenNodeTypes.NUMERIC_LITERAL; }

<YYINITIAL>           .                       { return CgTokenNodeTypes.BAD_CHARACTER; }