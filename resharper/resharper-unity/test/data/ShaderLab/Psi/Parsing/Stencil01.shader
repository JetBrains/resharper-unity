{caret}Shader "Unlit/SingleColor"
{
    SubShader
    {
        Pass {
            Stencil
            {
                Ref 255
                ReadMask 134
                WriteMask 155
                Comp Greater
                Pass Keep
                Fail Zero
                ZFail Replace
            }
        }
        Pass {
            Stencil
            {
                Ref 255
                ReadMask 134
                WriteMask 155
                Comp Less
                Pass IncrSat
                Fail DecrSat
                ZFail Invert
            }
        }
    }
}
