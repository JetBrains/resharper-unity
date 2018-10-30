using System;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;


namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [UnitTestProvider]
    public class UnityTestProvider : IUnitTestProvider
    {
        public static readonly IClrTypeName UnityTestAttribute = new ClrTypeName("UnityEngine.TestTools.UnityTestAttribute");
        
        public bool IsElementOfKind(IDeclaredElement declaredElement, UnitTestElementKind elementKind)
        {
            var isClass = declaredElement is ITypeElement;
            var isMethod = declaredElement is IFunction;
            
            switch (elementKind)
            {
                case UnitTestElementKind.Unknown:
                    return !isClass && !isMethod;
                case UnitTestElementKind.Test:
                    return isMethod && !isClass;
                case UnitTestElementKind.TestContainer:
                    return isClass;
                case UnitTestElementKind.TestStuff:
                    return isClass;
                default:
                    throw new ArgumentOutOfRangeException(nameof(elementKind), elementKind, null);
            }
        }

        public bool IsElementOfKind(IUnitTestElement element, UnitTestElementKind elementKind)
        {
            var declaredElement = element.GetDeclaredElement();
            if (declaredElement == null)
                return false;

            return IsElementOfKind(declaredElement, elementKind);
        }

        public bool IsSupported(IHostProvider hostProvider, IProject project, TargetFrameworkId targetFrameworkId)
        {
            return true;
        }

        public bool IsSupported(IProject project, TargetFrameworkId targetFrameworkId)
        {
            return true;
        }

        public int CompareUnitTestElements(IUnitTestElement x, IUnitTestElement y)
        {
            return String.CompareOrdinal(x.ShortName, y.ShortName);
        }

        public string ID => "UnityTest";

        public string Name => "UnityTest";
    }
}