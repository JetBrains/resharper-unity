{caret}
#if TRUE
float foo;
#endif

#if TRUE
float foo;
#elif TRUE
float foo;
#elif TRUE
float foo;
#endif

#if TRUE
float foo;
#else
float foo;
#endif

#if TRUE
float foo;
#elif TRUE
float foo;
#else
float foo;
#endif

#if DIRECTIVE_TEST

#define define_content
#undef undef_content
#include include_content
#line line_content
#error error_content
#warning warning_content
#pragma pragma_content

#endif endif_content