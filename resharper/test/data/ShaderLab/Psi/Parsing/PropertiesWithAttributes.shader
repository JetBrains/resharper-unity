{caret} Shader "test" {
  Properties {
    [HideInInspector] _WaveScale ("Wave scale", Range (0.02,0.15)) = 0.07 // sliders

    [KeywordEnum(None, Add, Multiply)] _Overlay("Overlay mode", Float) = 0

    // Make sure we can handle rogue whitespace. Note leading whitespace is invalid
    [   KeywordEnum  (None, Add, Multiply)] _Overlay2("Overlay mode 2", Float) = 0
    [KeywordEnum  (None, Add, Multiply)] _Overlay2("Overlay mode 2", Float) = 0
    [KeywordEnum  (None, Add, Multiply)  ] _Overlay2("Overlay mode 2", Float) = 0

    [Toggle] _Invert("Invert color?", Float) = 0

    // Blend mode values
    [Enum(UnityEngine.Rendering.BlendMode)] _Blend ("Blend mode", Float) = 1

    // A subset of blend mode values, just "One" (value 1) and "SrcAlpha" (value 5).
    [Enum(One,1,SrcAlpha,5)] _Blend2 ("Blend mode subset", Float) = 1

    // A slider with 3.0 response curve
    [PowerSlider(3.0)] _Shininess ("Shininess", Range (0.01, 1)) = 0.08

    [Header(Header Text)] 
    [Toggle] _Toggle("Toggle label text", Float) = 0.0
  }
}
