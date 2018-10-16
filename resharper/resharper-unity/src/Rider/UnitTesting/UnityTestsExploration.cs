using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Application;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.AttributeChecker;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class UnityTestsExploration
    {
        private readonly UnitTestAttributeCache myUnitTestAttributeCache;
        private readonly IProject myProject;
        private List<IMetadataMethod> myTempMethods;
        private JetHashSet<string> myTempMethodsNames;
        private readonly ReflectionTypeNameCache myTypeNameCache = new ReflectionTypeNameCache();
        private readonly UnityTestElementFactory myElementFactory;
        private readonly IUnitTestElementsObserver myUnitTestCollector;

        public UnityTestsExploration(UnitTestAttributeCache unitTestAttributeCache, IProject project,
            IUnitTestElementsObserver observer, IUnitTestElementIdFactory unitTestElementIdFactory,
            UnityTestProvider unityTestProvider, IUnitTestElementManager unitTestElementManager, UnityNUnitServiceProvider serviceProvider)
        {
            myUnitTestAttributeCache = unitTestAttributeCache;
            myProject = project;
            myUnitTestCollector = observer;
            myElementFactory = new UnityTestElementFactory(unitTestElementIdFactory, unityTestProvider, unitTestElementManager, serviceProvider);
        }

        public void Explore(IMetadataAssembly assembly, CancellationToken token)
        {
            try
            {
                myTempMethods = new List<IMetadataMethod>(200);
                myTempMethodsNames = new JetHashSet<string>(200);

                foreach (var type in assembly.GetTypes())
                    ExploreType(type, assembly, token);
            }

            finally
            {
                if (myTempMethods != null)
                {
                    myTempMethods.Clear();
                    myTempMethods = null;
                }
                if (myTempMethodsNames != null)
                {
                    myTempMethodsNames.Clear();
                    myTempMethods = null;
                }
            }
        }
        
        private void ExploreType(IMetadataTypeInfo type, IMetadataAssembly assembly, CancellationToken cancellationToken)
        {
            InterruptableActivityCookie.CheckAndThrow();
            cancellationToken.ThrowIfCancellationRequested();

            if (type.IsAbstract && type.GetMethods().Any(method => !(method.IsAbstract || method.IsStatic)))
                return;

            var testMethods = GetAllTestMethods(type);
            foreach (var method in testMethods)
            {
                var typeName = myTypeNameCache.GetClrName(method.DeclaringType);
                var id = string.Format("{0}.{1}", typeName, method.Name);

                var testElement = GetOrCreateTest(id, typeName, method.Name);
                myUnitTestCollector.OnUnitTestElement(testElement);
            }
        }
        
        private UnityTestElement GetOrCreateTest(string id, IClrTypeName typeName, string methodName)
        {
            var element = myElementFactory.GetOrCreateTest(id, myProject, myUnitTestCollector.TargetFrameworkId,  typeName, methodName);
            return element;
        }

        private List<IMetadataMethod> GetAllTestMethods(IMetadataTypeInfo type)
        {
            myTempMethods.Clear(); 
            myTempMethodsNames.Clear();
           
            var currentType = type;
            while (currentType != null)
            {
                foreach (var method in currentType.GetMethods())
                {
                    if (method.IsVirtual)
                    {
                        if (myTempMethodsNames.Contains(method.Name)) continue;
                        if (!method.IsNewSlot) myTempMethodsNames.Add(method.Name);
                    }          

                    if (IsTestMethod(method))
                        myTempMethods.Add(method);
                }

                var baseOfCurrentType = currentType.Base;
                currentType = (baseOfCurrentType != null) ? baseOfCurrentType.Type : null;
            }

            return myTempMethods;
        }
        
        private bool HasAttributeOrDerivedAttribute(IMetadataMethod method, params IClrTypeName[] attributeClrNames)
        {
            return myUnitTestAttributeCache.HasAttributeOrDerivedAttribute(myProject, method, attributeClrNames);
        }
        
        private bool IsTestMethod(IMetadataMethod method)
        {
            if (method.IsAbstract || !method.IsPublic) return false;
            return HasAttributeOrDerivedAttribute(method, UnityTestProvider.UnityTestAttribute);
        }
    }
}