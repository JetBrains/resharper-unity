{caret}Shader "Test"
{
    Properties
    {
        _Op("_Op",Float) = 0
	    _ColorOp("_ColorOp",Float) = 0
	    _AlphaOp("_AlphaOp",Float) = 0
    }
    
    SubShader
    {    
        BlendOp [_Op]
	    BlendOp Min	

	    BlendOp [_ColorOp], [_AlphaOp]
	    BlendOp [_ColorOp], Min
	    BlendOp Min, [_AlphaOp]
	    BlendOp Min, Min

	    BlendOp 1 [_ColorOp], [_AlphaOp]
	    BlendOp 1 [_ColorOp], Min
	    BlendOp 1 Min, [_AlphaOp]
	    BlendOp 1 Min, Min
	    BlendOp 1 [_Op]
	    BlendOp 1 Min

        Pass
        {
        }
    }
}