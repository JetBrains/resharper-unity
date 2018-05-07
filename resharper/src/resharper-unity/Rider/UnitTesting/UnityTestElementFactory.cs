using System;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Elements;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class UnityTestElementFactory
    {
        private readonly IUnitTestElementIdFactory myUnitTestElementIdFactory;
        private readonly object myLock = new object();
        private readonly WeakToWeakDictionary<UnitTestElementId, IUnitTestElement> myElements;
        private readonly IUnitTestElementManager myElementManager;
        private readonly UnityNUnitServiceProvider myServiceProvider;
        private readonly UnityTestProvider myUnitTestProvider;

        public UnityTestElementFactory(IUnitTestElementIdFactory unitTestElementIdFactory,
            UnityTestProvider unityTestProvider,
            IUnitTestElementManager elementManager, UnityNUnitServiceProvider serviceProvider)
        {
            myUnitTestElementIdFactory = unitTestElementIdFactory;
            myElementManager = elementManager;
            myServiceProvider = serviceProvider;
            myElements = new WeakToWeakDictionary<UnitTestElementId, IUnitTestElement>();
            myUnitTestProvider = unityTestProvider;
        }

        public UnityTestElement GetOrCreateTest(string id, [NotNull] IProject project, TargetFrameworkId targetFrameworkId, IClrTypeName typeName, string methodName)
        {
            lock (myLock)
            {
                var uid = myUnitTestElementIdFactory.Create(myUnitTestProvider, project, targetFrameworkId, id);
                var element = GetElementById<UnityTestElement>(uid);
                if (element == null)
                {
                    myElements[uid] = element = new UnityTestElement(project, typeName, uid, myServiceProvider, methodName);
                }

                return element;
            }
        }
        
        private T GetElementById<T>(UnitTestElementId id)
            where T : class, IUnitTestElement
        {
            IUnitTestElement element;
            if (myElements.TryGetValue(id, out element))
                return element as T;

            return myElementManager.GetElementById(id) as T;
        }
    }
}