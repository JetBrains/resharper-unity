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
COMMENT_CONTENT=({NOT_ASTERISK}|({ASTERISKS}{NOT_ASTERISK_OR_SLASH}))
UNFINISHED_DELIMITED_COMMENT={SLASH}{ASTERISK}{COMMENT_CONTENT}*
DELIMITED_COMMENT={UNFINISHED_DELIMITED_COMMENT}{ASTERISKS}{SLASH}

MINUS="-"
DOT="."
DECIMAL_DIGIT=[0-9]
INTEGER_LITERAL={MINUS}?{DECIMAL_DIGIT}+
FLOAT_LITERAL={MINUS}?{DECIMAL_DIGIT}*{DOT}{DECIMAL_DIGIT}*

%{ /* I don't think ShaderLab supports multi-line strings, or escape chars */
%}
QUOTE_CHAR="\""
STRING_LITERAL_CHAR=[^{QUOTE_CHAR}{NEW_LINE_CHARS}]
STRING_LITERAL={QUOTE_CHAR}{STRING_LITERAL_CHAR}*{QUOTE_CHAR}
UNFINISHED_STRING_LITERAL={QUOTE_CHAR}{STRING_LITERAL_CHAR}*

LETTER_CHAR={UNICODE_LL}|{UNICODE_LM}|{UNICODE_LO}|{UNICODE_LT}|{UNICODE_LU}|{UNICODE_NL}
IDENTIFIER_START_CHAR=("#"|"_"|{LETTER_CHAR})
IDENTIFIER_PART_CHAR=({IDENTIFIER_START_CHAR}|{DECIMAL_DIGIT})
IDENTIFIER=({IDENTIFIER_START_CHAR}{IDENTIFIER_PART_CHAR}*|"2D"|"3D")

CG_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^C])|(ENDC[^G]))+)
GLSL_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^G])|(ENDG[^L])|(ENDGL[^S])|(ENDGLS[^L]))+)
HLSL_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^H])|(ENDH[^L])|(ENDHL[^S])|(ENDHLS[^L]))+)

%state CGPROGRAM
%state GLSLPROGRAM
%state HLSLPROGRAM

%%

<YYINITIAL>   {WHITESPACE}            { return ShaderLabTokenType.WHITESPACE; }
<YYINITIAL>   {NEW_LINE}              { return ShaderLabTokenType.NEW_LINE; }

<YYINITIAL>   "="                     { return ShaderLabTokenType.EQUALS; }
<YYINITIAL>   ","                     { return ShaderLabTokenType.COMMA; }
<YYINITIAL>   "."                     { return ShaderLabTokenType.DOT; }
<YYINITIAL>   "+"                     { return ShaderLabTokenType.PLUS; }
<YYINITIAL>   "*"                     { return ShaderLabTokenType.MULTIPLY; }
<YYINITIAL>   "("                     { return ShaderLabTokenType.LPAREN; }
<YYINITIAL>   ")"                     { return ShaderLabTokenType.RPAREN; }
<YYINITIAL>   "{"                     { return ShaderLabTokenType.LBRACE; }
<YYINITIAL>   "}"                     { return ShaderLabTokenType.RBRACE; }
<YYINITIAL>   "["                     { return ShaderLabTokenType.LBRACK; }
<YYINITIAL>   "]"                     { return ShaderLabTokenType.RBRACK; }

<YYINITIAL>   "CGPROGRAM"             { yybegin(CGPROGRAM); return ShaderLabTokenType.CG_PROGRAM; }
<YYINITIAL>   "CGINCLUDE"             { yybegin(CGPROGRAM); return ShaderLabTokenType.CG_INCLUDE; }

<YYINITIAL>   "GLSLPROGRAM"           { yybegin(GLSLPROGRAM); return ShaderLabTokenType.GLSL_PROGRAM; }
<YYINITIAL>   "GLSLINCLUDE"           { yybegin(GLSLPROGRAM); return ShaderLabTokenType.GLSL_INCLUDE; }

<YYINITIAL>   "HLSLPROGRAM"           { yybegin(HLSLPROGRAM); return ShaderLabTokenType.HLSL_PROGRAM; }
<YYINITIAL>   "HLSLINCLUDE"           { yybegin(HLSLPROGRAM); return ShaderLabTokenType.HLSL_INCLUDE; }

<YYINITIAL>   {INTEGER_LITERAL}       { return ShaderLabTokenType.NUMERIC_LITERAL; }
<YYINITIAL>   {FLOAT_LITERAL}         { return ShaderLabTokenType.NUMERIC_LITERAL; }
<YYINITIAL>   {STRING_LITERAL}        { return ShaderLabTokenType.STRING_LITERAL; }

<YYINITIAL>   {UNFINISHED_STRING_LITERAL}        { return ShaderLabTokenType.STRING_LITERAL; }
<YYINITIAL>   {SINGLE_LINE_COMMENT}   { return ShaderLabTokenType.END_OF_LINE_COMMENT; }
<YYINITIAL>   {DELIMITED_COMMENT}     { return ShaderLabTokenType.MULTI_LINE_COMMENT; }
<YYINITIAL>   {UNFINISHED_DELIMITED_COMMENT}     { return ShaderLabTokenType.MULTI_LINE_COMMENT; }

<YYINITIAL>   {IDENTIFIER}            { return FindKeywordByCurrentToken() ?? ShaderLabTokenType.IDENTIFIER; }

<CGPROGRAM>   {CG_BLOCK}              { return ShaderLabTokenType.CG_CONTENT; }
<CGPROGRAM>   "ENDCG"                 { yybegin(YYINITIAL); return ShaderLabTokenType.CG_END; }

<GLSLPROGRAM> {GLSL_BLOCK}            { return ShaderLabTokenType.CG_CONTENT; }
<GLSLPROGRAM> "ENDGLSL"               { yybegin(YYINITIAL); return ShaderLabTokenType.GLSL_END; }

<HLSLPROGRAM> {HLSL_BLOCK}            { return ShaderLabTokenType.CG_CONTENT; }
<HLSLPROGRAM> "ENDHLSL"               { yybegin(YYINITIAL); return ShaderLabTokenType.HLSL_END; }

<YYINITIAL>   .                       { return ShaderLabTokenType.BAD_CHARACTER; }