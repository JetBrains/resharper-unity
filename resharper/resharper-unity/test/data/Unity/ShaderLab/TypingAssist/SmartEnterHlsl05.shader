// ${CHAR:Enter}
// ${SMART_INDENT_ON_ENTER:true} 
Shader "Sprites/Default-Hue"
{
    SubShader
    {        
        Pass
        {   
            CGPROGRAM            
                #pragma multi_compile BAR
            
                float3 hsv2rgb(float3 c)
                {
                  return c;{caret}                
                }
            ENDCG
        }
    }
}