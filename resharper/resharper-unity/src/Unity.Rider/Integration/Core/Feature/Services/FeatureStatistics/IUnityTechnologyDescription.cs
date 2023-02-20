using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Services.FeatureStatistics
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
}