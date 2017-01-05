using System;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

%%

%unicode

%init{
  currentTokenType = null;
%init}

%namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
%class ShaderLabLexerGenerated
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

INPUT_CHARACTER={NOT_NEW_LINE}

WHITESPACE_CHARS={UNICODE_ZS_CHARS}{UNICODE_CC_WHITESPACE_CHARS}{UNICODE_CF_WHITESPACE_CHARS}{NULL_CHAR}
VERTICAL_WHITESPACE_CHARS={UNICODE_CC_SEPARATOR_CHARS}{UNICODE_ZL_CHARS}{UNICODE_ZP_CHARS}
WHITESPACE_OPTIONAL=([{WHITESPACE_CHARS}])*
WHITESPACE=([{WHITESPACE_CHARS}])+

SLASH="/"
ASTERISK="*"
NOT_ASTERISK=[^{ASTERISK}]
ASTERISKS={ASTERISK}+

SINGLE_LINE_COMMENT=({SLASH}{SLASH}{INPUT_CHARACTER}*)
NOT_ASTERISK_OR_SLASH=[^{ASTERISK}{SLASH}]
COMMENT_CONTENT=({NOT_ASTERISK}|({ASTERISKS}{NOT_ASTERISK_OR_SLASH}*))
UNFINISHED_DELIMITED_COMMENT={SLASH}{ASTERISK}{COMMENT_CONTENT}*
DELIMITED_COMMENT={UNFINISHED_DELIMITED_COMMENT}{ASTERISKS}{SLASH}

MINUS="-"
DOT="."
DECIMAL_DIGIT=[0-9]
INTEGER_LITERAL={MINUS}?{DECIMAL_DIGIT}+
FLOAT_LITERAL={MINUS}?{DECIMAL_DIGIT}*{DOT}{DECIMAL_DIGIT}+

%{ /* I don't think ShaderLab supports multi-line strings, or escape chars */
%}
QUOTE_CHAR="\""
STRING_LITERAL_CHAR=[^{QUOTE_CHAR}{NEW_LINE_CHARS}]
STRING_LITERAL={QUOTE_CHAR}{STRING_LITERAL_CHAR}*{QUOTE_CHAR}
UNFINISHED_STRING_LITERAL={QUOTE_CHAR}{STRING_LITERAL_CHAR}*

UNDERSCORE="_"
LETTER_CHAR={UNICODE_LL}|{UNICODE_LM}|{UNICODE_LO}|{UNICODE_LT}|{UNICODE_LU}|{UNICODE_NL}
IDENTIFIER_START_CHAR=({UNDERSCORE}|{LETTER_CHAR})
IDENTIFIER_PART_CHAR=({IDENTIFIER_START_CHAR}|{DECIMAL_DIGIT})
IDENTIFIER=({IDENTIFIER_START_CHAR}{IDENTIFIER_PART_CHAR}*|"2D"|"3D")

CG_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^C])|(ENDC[^G]))+)

%state YYSHADERLAB
%state YYCGPROGRAM

%%

<YYSHADERLAB>   {WHITESPACE}            { return ShaderLabTokenType.WHITESPACE; }
<YYSHADERLAB>   {NEW_LINE}              { return ShaderLabTokenType.NEW_LINE; }

<YYSHADERLAB>   "="                     { return ShaderLabTokenType.EQUALS; }
<YYSHADERLAB>   ","                     { return ShaderLabTokenType.COMMA; }
<YYSHADERLAB>   "+"                     { return ShaderLabTokenType.PLUS; }
<YYSHADERLAB>   "*"                     { return ShaderLabTokenType.MULTIPLY; }
<YYSHADERLAB>   "("                     { return ShaderLabTokenType.LPAREN; }
<YYSHADERLAB>   ")"                     { return ShaderLabTokenType.RPAREN; }
<YYSHADERLAB>   "{"                     { return ShaderLabTokenType.LBRACE; }
<YYSHADERLAB>   "}"                     { return ShaderLabTokenType.RBRACE; }
<YYSHADERLAB>   "["                     { return ShaderLabTokenType.LBRACK; }
<YYSHADERLAB>   "]"                     { return ShaderLabTokenType.RBRACK; }

<YYSHADERLAB>   "CGPROGRAM"             { yybegin(YYCGPROGRAM); return ShaderLabTokenType.CG_PROGRAM; }
<YYSHADERLAB>   "CGINCLUDE"             { yybegin(YYCGPROGRAM); return ShaderLabTokenType.CG_INCLUDE; }

<YYSHADERLAB>   {INTEGER_LITERAL}       { return ShaderLabTokenType.NUMERIC_LITERAL; }
<YYSHADERLAB>   {FLOAT_LITERAL}         { return ShaderLabTokenType.NUMERIC_LITERAL; }
<YYSHADERLAB>   {STRING_LITERAL}        { return ShaderLabTokenType.STRING_LITERAL; }

<YYSHADERLAB>   {UNFINISHED_STRING_LITERAL}        { return ShaderLabTokenType.STRING_LITERAL; }
<YYSHADERLAB>   {SINGLE_LINE_COMMENT}   { return ShaderLabTokenType.END_OF_LINE_COMMENT; }
<YYSHADERLAB>   {DELIMITED_COMMENT}     { return ShaderLabTokenType.MULTI_LINE_COMMENT; }
<YYSHADERLAB>   {UNFINISHED_DELIMITED_COMMENT}     { return ShaderLabTokenType.MULTI_LINE_COMMENT; }

<YYSHADERLAB>   {IDENTIFIER}            { return FindKeywordByCurrentToken() ?? ShaderLabTokenType.IDENTIFIER; }

<YYCGPROGRAM>   {CG_BLOCK}              { return ShaderLabTokenType.CG_CONTENT; }
<YYCGPROGRAM>   "ENDCG"                 { yybegin(YYSHADERLAB); return ShaderLabTokenType.CG_END; }

<YYSHADERLAB>   .                       { return ShaderLabTokenType.BAD_CHARACTER; }