{caret}Shader "Examples/ExampleShader"
{
    SubShader
    {
        PackageRequirements
        {
            "com.my.package":
}
        Pass
        {
            PackageRequirements
            {
                : "[10.2.1,11.0]"
}
        }
}
}