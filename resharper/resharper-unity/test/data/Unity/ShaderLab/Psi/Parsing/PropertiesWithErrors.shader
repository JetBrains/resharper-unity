{caret} Shader "test" {
  Properties {
    ("Wave scale", Range (0.02,0.15)) = 0.07 // Missing name
    _MissingOpenBrace "Reflection distort", Range (0,1.5)) = 0.5
    _DisplayNameNotStringLiteral (1.0, Range (0,1.5)) = 0.4
    _DisplayNameMissing (, Range (0,1.5)) = 0.4
    _PropertyTypeIncorrect ("Whatever", Thing) = 0.5
    _PropertyTypeMissing ("Whatever", ) = 0.5
    _PropertyTypeMissing2 ("Whatever") = 0.5
    _TooManyParameters ("Whatever", Int, SomethingElse) = 0.5
    _RangeMissingOpenBrace ("Whatever", Range 0.3, 0.1)) = 0.3
    _RangeMissingFirstParameter ("Whatever", Range (, 0.1)) = 0.3
    _RangeFirstParameterIncorrectType ("Whatever", Range (Whatever, 0.1)) = 0.3
    _RangeMissingSecondParameter ("Whatever", Range (0.1,)) = 0.3
    _RangeSecondParameterIncorrectType ("Whatever", Range (0.1, Whatever)) = 0.3
    _RangeMissingClosingBrace ("Whatever", Range (0.1, 0.3) = 0.3
    _RangeMissingAllClosingBraces ("Whatever", Range (0.1, 0.3 = 0.3
    _ColorWithWrongType ("Whatever", Color) = (Thing, 0.3, .3, .3)
    _ColorWithMissingValue ("Whatever", Color) = (, 0.3, .3, .3)
    _ColorWithMissingOpenBrace ("Whatever", Color) = 0.3, 0.3, .3, .3)
    _ColorWithMissingClosingBrace ("Whatever", Color) = (0.3, 0.3, .3, .3
    _ColorWithTooManyValues ("Whatever", Color) = (0.3, 0.3, .3, .3, .3, .3)
    _MissingValue ("Whatever", Int) =
    _MissingEquals ("Whatever", Int) 0.2
    _TextureWithMissingName ("Whatever", 2D) = {}
    _TextureWithMissingBlock ("Whatever", 2D) = "Name"
    _TextureWithExtraBlockValues ("Whatever", 2D) = "Name" { Thing }
    _TextureWithExtraBlockValues2 ("Whatever", 2D) = "Name" { Thing, Whatever(), Stuff( ,,  }
  }
}
