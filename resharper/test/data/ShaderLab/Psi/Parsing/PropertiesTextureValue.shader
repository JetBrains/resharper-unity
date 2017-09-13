{caret}Shader "RIDER-8917" {
	Properties {	     
		_RenderTexture2 ("RenderTexture2", 2D) = "black" { 2D }
		_RenderTexture3 ("RenderTexture3", 2D) = "black" { 3D }
		_RenderTextureCube ("RenderTextureCube", 2D) = "black" { Cube }
		
		_RenderTextureTexGenCubeReflect ("RenderTextureTexGenCubeReflect", 2D) = "black" { TexGen CubeReflect }
		_RenderTextureTexGenCubeNormal ("RenderTextureTexGenCubeNormal", 2D) = "black" { TexGen CubeNormal }
		_RenderTextureTexGenObjectLinear ("RenderTextureTexGenObjectLinear", 2D) = "black" { TexGen ObjectLinear }
		_RenderTextureTexGenEyeLinear ("RenderTextureTexGenEyeLinear", 2D) = "black" { TexGen EyeLinear }
		_RenderTextureTexGenSphereMap ("RenderTextureTexGenSphereMap", 2D) = "black" { TEXGEN SphereMap }
		
		_RenderTextureMatrix ("RenderTextureMatrix", 2D) = "black" { Matrix [_Color] }
		
		_RenderTextureLightMap ("RenderTextureLigthMap", 2D) = "black" { LIGHTMAPMODE }				
	}
}