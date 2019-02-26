using System;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestProvider.nUnit.v30;
using JetBrains.Util.Dotnet.TargetFrameworkIds;


namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [UnitTestProvider]
    public class UnityTestProvider : IUnitTestProvider
    {
        private readonly NUnitTestProvider myNUnitTestProvider;

        public UnityTestProvider(NUnitTestProvider nUnitTestProvider)
        {
            myNUnitTestProvider = nUnitTestProvider;
        }
        
        public static readonly IClrTypeName UnityTestAttribute = new ClrTypeName("UnityEngine.TestTools.UnityTestAttribute");
        
        public bool IsElementOfKind(IDeclaredElement declaredElement, UnitTestElementKind elementKind)
        {
            return myNUnitTestProvider.IsElementOfKind(declaredElement, elementKind);
        }

        public bool IsElementOfKind(IUnitTestElement element, UnitTestElementKind elementKind)
        {
            return myNUnitTestProvider.IsElementOfKind(element, elementKind);
        }

        public bool IsSupported(IHostProvider hostProvider, IProject project, TargetFrameworkId targetFrameworkId)
        {
            return myNUnitTestProvider.IsSupported(hostProvider, project, targetFrameworkId);
        }

        public bool IsSupported(IProject project, TargetFrameworkId targetFrameworkId)
        {
            return myNUnitTestProvider.IsSupported(project, targetFrameworkId);
        }

        public int CompareUnitTestElements(IUnitTestElement x, IUnitTestElement y)
        {
            return String.CompareOrdinal(x.ShortName, y.ShortName);
        }

        public bool SupportsResultEventsFor(IUnitTestElement element)
        {
            return false;
        }

        public string ID => "UnityTest";

        public string Name => "UnityTest";
    }
}