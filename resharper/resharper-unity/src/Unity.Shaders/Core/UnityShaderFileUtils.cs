using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Core;

public static class UnityShaderFileUtils
{
    public static bool IsShaderLabFile(VirtualFileSystemPath path) => path.Name.EndsWith(ShaderLabProjectFileType.SHADERLAB_EXTENSION);
    
    public static bool IsComputeShaderFile(VirtualFileSystemPath path) => path.Name.EndsWith(CppProjectFileType.COMPUTE_EXTENSION) || IsUrtShaderFile(path);
    public static bool IsUrtShaderFile(VirtualFileSystemPath path) => path.Name.EndsWith(CppProjectFileType.URT_SHADER_EXTENSION);
    public static bool IsUrtShaderFile(CppFileLocation loc) => loc.Name.EndsWith(CppProjectFileType.URT_SHADER_EXTENSION);
}