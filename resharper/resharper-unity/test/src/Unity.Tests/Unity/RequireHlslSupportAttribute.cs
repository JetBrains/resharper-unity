using System;
using System.Linq;
using JetBrains.Application.Environment;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport;
using JetBrains.ReSharper.Resources.Shell;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace JetBrains.ReSharper.Plugins.Tests.Unity
{
    // HLSL requires Psi.Cpp, which doesn't work on Mono. This attribute will ignore any test if
    // ILanguageHlslSupportZone is not active
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
    public class RequireHlslSupportAttribute : TestActionAttribute
    {
        public override void BeforeTest(ITest test)
        {
            if (test.RunState == RunState.NotRunnable)
                return;

            var productConfigurations = Shell.Instance.GetComponent<RunsProducts.ProductConfigurations>();

        }
    }
}