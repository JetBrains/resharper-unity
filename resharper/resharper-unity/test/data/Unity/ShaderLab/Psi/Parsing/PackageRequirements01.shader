{caret}Shader "Examples/ExampleShader"
{
    SubShader
    {
        PackageRequirements
        {
            "com.my.package": "2.2"
            "com.package.without.version"
        }
        Pass
        {
            PackageRequirements
            {
                "com.unity.render-pipelines.universal": "[10.2.1,11.0]"
                "com.unity.textmeshpro": "3.2"
            }
        }
        Pass
        {
            PackageRequirements
            {
                "com.unity.render-pipelines.high-definition": "[8.0,8.5]"
            }
        }
    }
}