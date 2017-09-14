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

SINGLE_LINE_COMMENT=({SLASH}{SLASH}{INPUT_CHARACTER}*)
NOT_ASTERISK_OR_SLASH=[^{ASTERISK}{SLASH}]
COMMENT_CONTENT=({NOT_ASTERISK}|({ASTERISKS}{NOT_ASTERISK_OR_SLASH}))
UNFINISHED_DELIMITED_COMMENT={SLASH}{ASTERISK}{COMMENT_CONTENT}*
DELIMITED_COMMENT={UNFINISHED_DELIMITED_COMMENT}{ASTERISKS}{SLASH}

MINUS="-"
PLUS="+"
DECIMAL_DIGIT=[0-9]
NUMERIC_LITERAL=({MINUS}|{PLUS})?{DECIMAL_DIGIT}+

UNDERSCORE="_"
LETTER_CHAR={UNICODE_LL}|{UNICODE_LM}|{UNICODE_LO}|{UNICODE_LT}|{UNICODE_LU}|{UNICODE_NL}
IDENTIFIER_START_CHAR=({UNDERSCORE}|{LETTER_CHAR})
IDENTIFIER_PART_CHAR=({IDENTIFIER_START_CHAR}|{DECIMAL_DIGIT})
IDENTIFIER=({IDENTIFIER_START_CHAR}{IDENTIFIER_PART_CHAR}*)

NON_EOL_WS=(([\ \t])|({LINE_CONTINUATOR}))
PRP=("#"({NON_EOL_WS}|{DELIMITED_COMMENT}|{UNFINISHED_DELIMITED_COMMENT})*)

IF_DIRECTIVE=({PRP}"if")
IFDEF_DIRECTIVE=({PRP}"ifdef")
IFNDEF_DIRECTIVE=({PRP}"ifndef")
ELIF_DIRECTIVE=({PRP}"elif")
ELSE_DIRECTIVE=({PRP}"else")
ENDIF_DIRECTIVE=({PRP}"endif")
INCLUDE_DIRECTIVE=({PRP}"include")
DEFINE_DIRECTIVE=({PRP}"define")
UNDEF_DIRECTIVE=({PRP}"undef")
LINE_DIRECTIVE=({PRP}"line")
ERROR_DIRECTIVE=({PRP}"error")
WARNING_DIRECTIVE=({PRP}"warning")
PRAGMA_DIRECTIVE=({PRP}"pragma")
UNKNOWN_DIRECTIVE=({PRP}{IDENTIFIER})

%{ /*no empty directive support*/
%}

SLASH_AND_NOT_SLASH=("/"[^\/\u0085\u2028\u2029\u000D\u000A])
NOT_SLASH_NOT_NEW_LINE=([^\/\u0085\u2028\u2029\u000D\u000A])
DIRECTIVE_CONTENT=(({LINE_CONTINUATOR}|{DELIMITED_COMMENT}|{SLASH_AND_NOT_SLASH}|{NOT_SLASH_NOT_NEW_LINE})*)(\/|{SINGLE_LINE_COMMENT})?

%state DIRECTIVE
%state CODE_DIRECTIVE
%state INCLUDE_DIRECTIVE

%%

<YYINITIAL>           {WHITESPACE}            { return CgTokenNodeTypes.WHITESPACE; }
<YYINITIAL>           {NEW_LINE}              { return CgTokenNodeTypes.NEW_LINE; }

<DIRECTIVE>           {DIRECTIVE_CONTENT}     { yybegin(YYINITIAL);         return CgTokenNodeTypes.DIRECTIVE_CONTENT;          }
<CODE_DIRECTIVE>      {DIRECTIVE_CONTENT}     { yybegin(YYINITIAL);         return CgTokenNodeTypes.CODE_DIRECTIVE_CONTENT;     }
<INCLUDE_DIRECTIVE>   {DIRECTIVE_CONTENT}     { yybegin(YYINITIAL);         return CgTokenNodeTypes.INCLUDE_DIRECTIVE_CONTENT;  }

<YYINITIAL>           {IF_DIRECTIVE}          { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.IF_DIRECTIVE;               }
<YYINITIAL>           {IFDEF_DIRECTIVE}       { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.IFDEF_DIRECTIVE;            }
<YYINITIAL>           {IFNDEF_DIRECTIVE}      { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.IFNDEF_DIRECTIVE;           }
<YYINITIAL>           {ELIF_DIRECTIVE}        { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.ELIF_DIRECTIVE;             }
<YYINITIAL>           {ELSE_DIRECTIVE}        { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.ELSE_DIRECTIVE;             }
<YYINITIAL>           {ENDIF_DIRECTIVE}       { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.ENDIF_DIRECTIVE;            }
<YYINITIAL>           {INCLUDE_DIRECTIVE}     { yybegin(INCLUDE_DIRECTIVE); return CgTokenNodeTypes.INCLUDE_DIRECTIVE;           }
<YYINITIAL>           {DEFINE_DIRECTIVE}      { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.DEFINE_DIRECTIVE;           }
<YYINITIAL>           {UNDEF_DIRECTIVE}       { yybegin(CODE_DIRECTIVE);    return CgTokenNodeTypes.UNDEF_DIRECTIVE;            }
<YYINITIAL>           {LINE_DIRECTIVE}        { yybegin(DIRECTIVE);         return CgTokenNodeTypes.LINE_DIRECTIVE;             }
<YYINITIAL>           {ERROR_DIRECTIVE}       { yybegin(DIRECTIVE);         return CgTokenNodeTypes.ERROR_DIRECTIVE;            }
<YYINITIAL>           {WARNING_DIRECTIVE}     { yybegin(DIRECTIVE);         return CgTokenNodeTypes.WARNING_DIRECTIVE;          }
<YYINITIAL>           {PRAGMA_DIRECTIVE}      { yybegin(DIRECTIVE);         return CgTokenNodeTypes.PRAGMA_DIRECTIVE;           }

<YYINITIAL>           "{"                     { return CgTokenNodeTypes.LBRACE; }
<YYINITIAL>           "}"                     { return CgTokenNodeTypes.RBRACE; }
<YYINITIAL>           "("                     { return CgTokenNodeTypes.LPAREN; }
<YYINITIAL>           ")"                     { return CgTokenNodeTypes.RPAREN; }
<YYINITIAL>           "."                     { return CgTokenNodeTypes.DOT; }
<YYINITIAL>           ","                     { return CgTokenNodeTypes.COMMA; }
<YYINITIAL>           ";"                     { return CgTokenNodeTypes.SEMICOLON; }
<YYINITIAL>           ":"                     { return CgTokenNodeTypes.COLON; }
<YYINITIAL>           "="                     { return CgTokenNodeTypes.EQUALS; }

<YYINITIAL>           {SINGLE_LINE_COMMENT}   { return CgTokenNodeTypes.SINGLE_LINE_COMMENT; }

<YYINITIAL>           {IDENTIFIER}            { return FindKeywordByCurrentToken() ?? CgTokenNodeTypes.IDENTIFIER; }
<YYINITIAL>           {NUMERIC_LITERAL}       { return CgTokenNodeTypes.NUMERIC_LITERAL; }

<YYINITIAL>           .                       { return CgTokenNodeTypes.BAD_CHARACTER; }