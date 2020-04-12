{caret}Shader "Unlit/SingleColor"
{
    SubShader
    {
        Pass {
            Material {
                Diffuse (1,1,1,1)
                Ambient (1,1,1,1)
            }
            Lighting On
        }
    }
}
