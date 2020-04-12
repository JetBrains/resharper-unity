using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  public class YamlLexerFactory : ILexerFactory
  {
    public ILexer CreateLexer(IBuffer buffer)
    {
      // Another Unity work around. The Unity code using the YAML PSI checks that the Unity project will "force text"
      // serialisation, but some files are still serialised as binary, notably NavMesh.asset, LightingData.asset
      // (possibly terrain data) and
      if (IsBinaryBuffer(buffer))
        return new YamlBinaryLexer(buffer);
      return GetDefaultLexer(buffer);
    }

    protected virtual ILexer GetDefaultLexer(IBuffer buffer) => new YamlLexer(buffer, true, false);

    private bool IsBinaryBuffer(IBuffer buffer)
    {
      for (var i = 0; i < 20 && i < buffer.Length; i++)
      {
        var c = buffer[i];
        if (!IsValidYamlChar(c))
          return true;
      }

      return false;
    }

    private bool IsValidYamlChar(char c)
    {
      // [1]	c-printable	::=	  #x9 | #xA | #xD | [#x20-#x7E]          /* 8 bit */
      //                        | #x85 | [#xA0-#xD7FF] | [#xE000-#xFFFD] /* 16 bit */
      //                        | [#x10000-#x10FFFF]                     /* 32 bit */
      // char is only 16 bit
      return c == 0x9 || c == 0xA || c == 0xD || (c >= 0x20 && c <= 0x7E) || c == 0x85 || (c >= 0xA0 && c <= 0xD7FF)
             || (c >= 0xE000 && c <= 0xFFFD);
    }

    private class YamlBinaryLexer : ILexer
    {
      public YamlBinaryLexer(IBuffer buffer)
      {
        Buffer = buffer;
      }

      public void Start()
      {
        TokenType = YamlTokenType.NON_PRINTABLE;
      }

      public void Advance()
      {
        TokenType = null;
      }

      public object CurrentPosition { get; set; }
      public TokenNodeType TokenType { get; private set; }
      public int TokenStart => 0;
      public int TokenEnd => Buffer.Length;
      public IBuffer Buffer { get; }
    }
  }
}