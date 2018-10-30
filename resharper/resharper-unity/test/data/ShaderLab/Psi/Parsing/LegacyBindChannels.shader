{caret}Shader "Solid Red"
{
    SubShader
    {
        Pass
        {
            BindChannels {
               Bind "Vertex", vertex
               Bind "texcoord", texcoord0
               Bind "texcoord1", texcoord1
            }
        }
    }
}
