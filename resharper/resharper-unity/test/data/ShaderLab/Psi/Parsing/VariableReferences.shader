{caret}Shader "Test"
{
    Properties
    {
        _Op("Op", float) = 1
    }
    
    SubShader
    {    
        BlendOp [_Op]

        // Variable references have the same syntax as attributes
	    BlendOp [_Op ]
        BlendOp [_Op(Something something something)]
        BlendOp [_Op(Something something something   )]
        BlendOp [_Op(Something something something   )   ]

        Pass
        {
        }
    }
}
