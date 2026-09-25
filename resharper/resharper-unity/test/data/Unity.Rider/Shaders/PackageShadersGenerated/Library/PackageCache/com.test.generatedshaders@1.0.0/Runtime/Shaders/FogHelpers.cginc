struct FogParams
{
    float Density;
    float3 Tint;
};

float3 ApplyFog(FogParams fogParams, float3 sourceColour, float depth)
{
    float amount = 1.0 - exp(-depth * fogParams.Density);
    return lerp(sourceColour, fogParams.Tint, amount);
}
