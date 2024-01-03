Shader "MyShader"
{
    SubShader
    {
        Name "Sub{caret}1"
        Blend One One
    }
    
    SubShader
    {
        Name "Sub2"
        Blend DstColor Zero
    }
}