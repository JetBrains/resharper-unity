using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies
{
    public interface IUnityTechnologyDescription
    {
        public string Id { get; }
        public IEnumerable<string> GetPossiblePackageName();
        public IEnumerable<string> GetPossibleAssemblyName();
        public IEnumerable<string> GetPossibleProjectName();
    }

    public class HDRPUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "HDRP";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return "com.unity.render-pipelines.high-definition";
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }
    }

    public class CoreRPUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "CoreRP";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return "com.unity.render-pipelines.core";
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }
    }

    public class URPUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "URP";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return "com.unity.render-pipelines.universal";
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }

    }

    public class EntitiesUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "ECS";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return PackageManager.UnityEntitiesPackageName;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }
    }

    public class InputSystemUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "InputSystem";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return "com.unity.inputsystem";
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }
    }

    public class BurstUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "Burst";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return "com.unity.burst";
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }
    }

    public class OdinUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "Odin";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield return "Sirenix.OdinInspector.Attributes";
            yield return "Sirenix.Serialization";
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield return "Sirenix.OdinInspector.Attributes";
            yield return "Sirenix.Serialization";
        }
    }
    
    public class PhotonUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "Photon";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield return "Photon3Unity3D.dll";
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield return "PhotonUnityNetworking";
            yield return "PhotonRealtime";
        }
    }


    public class PeekUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "Peek";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield return "Ludiq.Peek.Editor";
            yield return "Ludiq.PeekCore.Editor";
        }
    }

    public class UniRxUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "UniRx";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield return "UniRx";
        }
    }

    public class UniTaskUnityTechnologyDescription : IUnityTechnologyDescription
    {
        public string Id => "UniTask";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield return "UniTask";
        }
    }
    
    public class UnityTestFrameworkDescription : IUnityTechnologyDescription
    {
        public string Id => "TestFramework";

        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return "com.unity.test-framework";
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield return "UnityEngine.TestRunner";
        }
    }

    public class PackageBasedUnityTechnologyDescription : IUnityTechnologyDescription
    {
        private readonly string myPackageId;
        public string Id { get; }

        public PackageBasedUnityTechnologyDescription(string id, string packageId)
        {
            myPackageId = packageId;
            Id = id;
        }
        public IEnumerable<string> GetPossiblePackageName()
        {
            yield return myPackageId;
        }

        public IEnumerable<string> GetPossibleAssemblyName()
        {
            yield break;
        }

        public IEnumerable<string> GetPossibleProjectName()
        {
            yield break;
        }
    }

    public class PythonScriptingUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public PythonScriptingUnityTechnologyDescription() : base("PythonScripting", "com.unity.scripting.python")
        {
        }
    }
    
    public class AddressablesUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public AddressablesUnityTechnologyDescription() : base("Addressables", "com.unity.addressables")
        {
        }
    }
    
    public class AndroidLogCatUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public AndroidLogCatUnityTechnologyDescription() : base("AndroidLogCat", "com.unity.mobile.android-logcat")
        {
        }
    }
    
    public class CodeCoverageUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public CodeCoverageUnityTechnologyDescription() : base("CodeCoverage", "com.unity.testtools.codecoverage")
        {
        }
    }
    
    public class UnityCollectionsUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public UnityCollectionsUnityTechnologyDescription() : base("UnityCollections", "com.unity.collections")
        {
        }
    }
    
    public class EditorCoroutinesUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public EditorCoroutinesUnityTechnologyDescription() : base("EditorCoroutines", "com.unity.editorcoroutines")
        {
        }
    }
    
    public class EntitiesGraphicsUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public EntitiesGraphicsUnityTechnologyDescription() : base("EntitiesGraphics", "com.unity.entities.graphics")
        {
        }
    }
    
    public class LocalizationUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public LocalizationUnityTechnologyDescription() : base("Localization", "com.unity.localization")
        {
        }
    }
    
    public class MathematicsUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public MathematicsUnityTechnologyDescription() : base("Mathematics", "com.unity.mathematics")
        {
        }
    }
    
    public class TransportUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public TransportUnityTechnologyDescription() : base("Transport", "com.unity.transport")
        {
        }
    }
    
    public class UnityPhysicsUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public UnityPhysicsUnityTechnologyDescription() : base("UnityPhysics", "com.unity.physics")
        {
        }
    }
    
    public class HavokUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public HavokUnityTechnologyDescription() : base("Havok", "com.havok.physics")
        {
        }
    }
    
    public class MlAgentsUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public MlAgentsUnityTechnologyDescription() : base("MlAgents", "com.unity.ml-agents")
        {
        }
    }
    
    public class MultiplayerToolsTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public MultiplayerToolsTechnologyDescription() : base("MultiplayerTools", "com.unity.multiplayer.tools")
        {
        }
    }
    
    public class NetCodeUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public NetCodeUnityTechnologyDescription() : base("NetCode", "com.unity.netcode")
        {
        }
    }
    
    public class NetCodeGameObjectsUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public NetCodeGameObjectsUnityTechnologyDescription() : base("NetCodeGameObjects", "com.unity.netcode.gameobjects")
        {
        }
    }
    
    
    public class SerializationUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public SerializationUnityTechnologyDescription() : base("Serialization", "com.unity.serialization")
        {
        }
    }
    
    public class LoggingUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public LoggingUnityTechnologyDescription() : base("Logging", "com.unity.logging")
        {
        }
    }
    
    public class MemoryProfilerUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public MemoryProfilerUnityTechnologyDescription() : base("MemoryProfiler", "com.unity.memoryprofiler")
        {
        }
    }
    
    public class ProfilerAnalyzerUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public ProfilerAnalyzerUnityTechnologyDescription() : base("ProfilerAnalyzer", "com.unity.performance.profile-analyzer")
        {
        }
    }
    
    
    public class ProfilingCoreUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public ProfilingCoreUnityTechnologyDescription() : base("ProfilerCore", "com.unity.profiling.core")
        {
        }
    }
    
    public class CollabUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public CollabUnityTechnologyDescription() : base("Collab", "com.unity.collab-proxy")
        {
        }
    }
    
    public class VisualScriptingUnityTechnologyDescription : PackageBasedUnityTechnologyDescription
    {
        public VisualScriptingUnityTechnologyDescription() : base("VisualScripting", "com.unity.visualscripting")
        {
        }
    }
}