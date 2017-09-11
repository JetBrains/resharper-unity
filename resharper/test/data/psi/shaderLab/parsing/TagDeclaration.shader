{caret}Shader "Test"
{    
    SubShader{        
        Pass{
            Tags { LightMode = Vertex }
            Tags { "LightMode" = "Vertex" }

            // errors:
            Tags { LightMode = "Vertex" }
            Tags { "LightMode" = Vertex }
            CGPROGRAM
            ENDCG
        }
    }
}