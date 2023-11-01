namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Model;

public enum ShaderApi
{
    D3D11, //Direct3D 11
    GlCore, //Desktop OpenGL "core" (GL 3/4)
    GlEs, //OpenGL ES 2.0
    GlEs3, //OpenGL ES 3.0/3.1
    Metal, //iOS/Mac Metal
    Vulkan, //Vulkan
    D3D11L9X, //Direct3D 11 "feature level 9.x" target for Universal Windows Platform
    Desktop, //Windows, Mac and Linux desktop platforms, WebGL, Stadia
    Mobile, //iOS and Android mobile platforms, tvOS
}