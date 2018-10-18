// Colored vertex lighting

#warning This is a warning message
#error This is an error message
#line 23

Shader "MyShader"
{
  #warning Another warning message
  #error Error messages can go anywhere!!

  // Technically allowed, but the lexer swallows the first non-digit char
  #line 45PProperties {
  }

  // The lexer allows the line number to contain whitespace
  #line 34 24 22

  SubShader {

    // Errors!
    #error
    #warning
    #line
    #whatever
  }
}

#warning One more warning message
#error Error message with @&!&^(^)_%||'' /* weird characters!!
#line 45
