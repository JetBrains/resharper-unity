{caret} Shader "test" {
  Properties {
    [Header()()] _Value3("Whatever", Float) = 1
    [Header((inactive))] _Value3("Whatever", Float) = 1
    [Header()] _Value3("Whatever", Float) = 1
  }
}
