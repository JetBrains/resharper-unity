Shader "Unlit/SingleColor"
{
    Properties
    {
        _Op ("Op value", float) = 1
    }
    SubShader
    {
        BlendOp [_Op]
        BlendOp [_Op ]
        BlendOp [_Op (something something) ]
        BlendOp [_Op (something, something) ]
        BlendOp [_Op (something, something ]
    }
}
