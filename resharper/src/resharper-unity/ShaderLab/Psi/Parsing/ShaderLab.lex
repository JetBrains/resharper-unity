using System;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;

%%

%unicode

%init{
  currentTokenType = null;
%init}

%namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
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
MULTI_LINE_COMMENT_START={SLASH}{ASTERISK}

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

LPAREN="("
RPAREN=")"
UNQUOTED_STRING_LITERAL_START_CHAR=("#"|"_"|{LETTER_CHAR})
UNQUOTED_STRING_LITERAL_PART_CHAR=({UNQUOTED_STRING_LITERAL_START_CHAR}|{DECIMAL_DIGIT}|{WHITESPACE}|{DOT}|{LPAREN}|{RPAREN})
UNQUOTED_STRING_LITERAL_END_CHAR=({UNQUOTED_STRING_LITERAL_START_CHAR}|{DECIMAL_DIGIT}|{DOT}|{LPAREN})
UNQUOTED_STRING_LITERAL={IDENTIFIER_START_CHAR}({UNQUOTED_STRING_LITERAL_END_CHAR}?|{UNQUOTED_STRING_LITERAL_PART_CHAR}+{UNQUOTED_STRING_LITERAL_END_CHAR})

IDENTIFIER_START_CHAR=("#"|"_"|{LETTER_CHAR})
IDENTIFIER_PART_CHAR=({IDENTIFIER_START_CHAR}|{DECIMAL_DIGIT})
IDENTIFIER={IDENTIFIER_START_CHAR}{IDENTIFIER_PART_CHAR}*

CG_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^C])|(ENDC[^G]))+)
GLSL_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^G])|(ENDG[^L])|(ENDGL[^S])|(ENDGLS[^L]))+)
HLSL_BLOCK=(([^E]|(E[^N])|(EN[^D])|(END[^H])|(ENDH[^L])|(ENDHL[^S])|(ENDHLS[^L]))+)

PP_MESSAGE={NOT_NEW_LINE}*
PP_DIGITS={DECIMAL_DIGIT}(({WHITESPACE})*{DECIMAL_DIGIT})*

%state PPMESSAGE, PPDIGITS
%state CGPROGRAM, GLSLPROGRAM,HLSLPROGRAM
%state BRACKETS, PARENS

%%

<YYINITIAL,PPMESSAGE,PPDIGITS,BRACKETS,PARENS>
               {WHITESPACE}                     { return ShaderLabTokenType.WHITESPACE; }
<YYINITIAL>    {NEW_LINE}                       { return ShaderLabTokenType.NEW_LINE; }

<YYINITIAL>     "="                             { return ShaderLabTokenType.EQUALS; }
<YYINITIAL,PARENS>          ","                 { return ShaderLabTokenType.COMMA; }
<YYINITIAL>     "."                             { return ShaderLabTokenType.DOT; }
<YYINITIAL>     "+"                             { return ShaderLabTokenType.PLUS; }
<YYINITIAL>     "-"                             { return ShaderLabTokenType.MINUS; }
<YYINITIAL>     "+-"                            { return ShaderLabTokenType.PLUS_MINUS; }
<YYINITIAL>     "*"                             { return ShaderLabTokenType.MULTIPLY; }
<YYINITIAL>     "{"                             { return ShaderLabTokenType.LBRACE; }
<YYINITIAL>     "}"                             { return ShaderLabTokenType.RBRACE; }

<YYINITIAL>     "("                             { return ShaderLabTokenType.LPAREN; }
<YYINITIAL,BRACKETS,PARENS> ")"                 { return ShaderLabTokenType.RPAREN; }

<YYINITIAL>     "["                             { yybegin(BRACKETS); return ShaderLabTokenType.LBRACK; }
<YYINITIAL,BRACKETS,PARENS> "]"                 { yybegin(YYINITIAL); return ShaderLabTokenType.RBRACK; }

<BRACKETS>      "("                             { yybegin(PARENS); return ShaderLabTokenType.LPAREN; }
<PARENS>        {UNQUOTED_STRING_LITERAL}       { return ShaderLabTokenType.UNQUOTED_STRING_LITERAL; }
<BRACKETS,PARENS>           {NEW_LINE}          { yybegin(YYINITIAL); return ShaderLabTokenType.NEW_LINE; }

<YYINITIAL>     "#warning"                      { yybegin(PPMESSAGE); return ShaderLabTokenType.PP_WARNING; }
<YYINITIAL>     "#error"                        { yybegin(PPMESSAGE); return ShaderLabTokenType.PP_ERROR; }
<YYINITIAL>     "#line"                         { yybegin(PPDIGITS); return ShaderLabTokenType.PP_LINE; }

<PPMESSAGE>     {PP_MESSAGE}                    { yybegin(YYINITIAL); return ShaderLabTokenType.PP_MESSAGE; }
<PPMESSAGE>     {NEW_LINE}                      { yybegin(YYINITIAL); return ShaderLabTokenType.NEW_LINE; }

<PPDIGITS>      {PP_DIGITS}                     { /* Digits can contain whitespace. Lexing continues at end, but next char is swallowed */ return ShaderLabTokenType.PP_DIGITS; }
<PPDIGITS>      {NEW_LINE}                      { yybegin(YYINITIAL); return ShaderLabTokenType.NEW_LINE; }
<PPDIGITS>      .                               { yybegin(YYINITIAL); return ShaderLabTokenType.PP_SWALLOWED; }

<YYINITIAL>     "CGPROGRAM"                     { yybegin(CGPROGRAM); return ShaderLabTokenType.CG_PROGRAM; }
<YYINITIAL>     "CGINCLUDE"                     { yybegin(CGPROGRAM); return ShaderLabTokenType.CG_INCLUDE; }

<YYINITIAL>     "GLSLPROGRAM"                   { yybegin(GLSLPROGRAM); return ShaderLabTokenType.GLSL_PROGRAM; }
<YYINITIAL>     "GLSLINCLUDE"                   { yybegin(GLSLPROGRAM); return ShaderLabTokenType.GLSL_INCLUDE; }

<YYINITIAL>     "HLSLPROGRAM"                   { yybegin(HLSLPROGRAM); return ShaderLabTokenType.HLSL_PROGRAM; }
<YYINITIAL>     "HLSLINCLUDE"                   { yybegin(HLSLPROGRAM); return ShaderLabTokenType.HLSL_INCLUDE; }

<YYINITIAL,BRACKETS,PARENS> {INTEGER_LITERAL}   { return ShaderLabTokenType.NUMERIC_LITERAL; }
<YYINITIAL,BRACKETS,PARENS> {FLOAT_LITERAL}     { return ShaderLabTokenType.NUMERIC_LITERAL; }
<YYINITIAL,BRACKETS>        {STRING_LITERAL}    { return ShaderLabTokenType.STRING_LITERAL; }
<YYINITIAL,BRACKETS>        {UNFINISHED_STRING_LITERAL} { return ShaderLabTokenType.STRING_LITERAL; }

<YYINITIAL>     {SINGLE_LINE_COMMENT}           { return ShaderLabTokenType.END_OF_LINE_COMMENT; }
<YYINITIAL>     {MULTI_LINE_COMMENT_START}      { return HandleNestedMultiLineComment(); }

<YYINITIAL,BRACKETS,PARENS> {IDENTIFIER}        { return FindKeywordByCurrentToken() ?? ShaderLabTokenType.IDENTIFIER; }
<YYINITIAL,BRACKETS,PARENS> "2D"                { return ShaderLabTokenType.TEXTURE_2D_KEYWORD; }
<YYINITIAL,BRACKETS,PARENS> "2DArray"           { return ShaderLabTokenType.TEXTURE_2D_ARRAY_KEYWORD; }
<YYINITIAL,BRACKETS,PARENS> "3D"                { return ShaderLabTokenType.TEXTURE_3D_KEYWORD; }

<CGPROGRAM>     {CG_BLOCK}                      { return ShaderLabTokenType.CG_CONTENT; }
<CGPROGRAM>     "ENDCG"                         { yybegin(YYINITIAL); return ShaderLabTokenType.CG_END; }

<GLSLPROGRAM>   {GLSL_BLOCK}                    { return ShaderLabTokenType.CG_CONTENT; }
<GLSLPROGRAM>   "ENDGLSL"                       { yybegin(YYINITIAL); return ShaderLabTokenType.GLSL_END; }

<HLSLPROGRAM>   {HLSL_BLOCK}                    { return ShaderLabTokenType.CG_CONTENT; }
<HLSLPROGRAM>   "ENDHLSL"                       { yybegin(YYINITIAL); return ShaderLabTokenType.HLSL_END; }

<YYINITIAL,BRACKETS,PARENS> .                   { return ShaderLabTokenType.BAD_CHARACTER; }