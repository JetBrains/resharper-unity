using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.ReSharper.UnitTestFramework.Strategy;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class UnityTestElement : IUnitTestElement
    {
        private readonly UnitTestingCachingService myUnitTestingCachingService;
        private readonly IProject myProject;
        private readonly IClrTypeName myClrTypeName;
        private readonly UnitTestElementId myId;
        private readonly UnityNUnitServiceProvider myUnityNUnitServiceProvider;
        private readonly string myMethodName;
        private ISet<UnitTestElementCategory> myCategories;
        private string myExplicitReason;

        public UnityTestElement([NotNull] IProject project, [NotNull] IClrTypeName clrTypeName, UnitTestElementId id, UnityNUnitServiceProvider unityNUnitServiceProvider, string methodName)
        {
            myUnitTestingCachingService = unityNUnitServiceProvider.CachingService;
            myProject = project;
            myClrTypeName = clrTypeName;
            myId = id;
            myUnityNUnitServiceProvider = unityNUnitServiceProvider;
            myMethodName = methodName;
        }
        
        public string GetPresentation(IUnitTestElement parent = null, bool full = false)
        {
            return myClrTypeName.FullName;
        }

        public UnitTestElementNamespace GetNamespace()
        {
            var validDeclaredElement = GetTypeElement();

            var containingType = validDeclaredElement?.GetContainingType();
            if (containingType == null)
                return null;

            return UnitTestElementNamespace.Create(containingType.GetClrName().GetNamespaceName());
        }

        private ITypeElement GetTypeElement()
        {
            if (myProject.IsValid())
                return myUnitTestingCachingService.GetTypeElement(myProject,
                    myProject.GetCurrentTargetFrameworkId(), myClrTypeName, true, false);
            return null;
        }

        public UnitTestElementDisposition GetDisposition()
        {
            var element = GetDeclaredElement();
            if (element != null && element.IsValid())
            {
                var locations = new List<UnitTestElementLocation>();
                foreach (var declaration in element.GetDeclarations())
                {
                    var file = declaration.GetContainingFile();
                    if (file != null)
                        locations.Add(new UnitTestElementLocation(file.GetSourceFile().ToProjectFile(), declaration.GetNameDocumentRange().TextRange, declaration.GetDocumentRange().TextRange));
                }
                return new UnitTestElementDisposition(locations, this);
            }
            return UnitTestElementDisposition.InvalidDisposition;
        }

        public IDeclaredElement GetDeclaredElement()
        {
            var typeElement = GetTypeElement();
            if (typeElement == null)
                return null;

            if (!myProject.IsValid())
                return null;
            
            using (CompilationContextCookie.GetOrCreate(myProject.GetResolveContext()))
            {
                foreach (var member in typeElement.EnumerateMembers(myMethodName, typeElement.CaseSensitiveName))
                {
                    var method = member as IMethod;
                    if (method == null)
                        continue;
                    if (method.IsAbstract)
                        continue;
                    if (method.TypeParameters.Count > 0)
                        continue;

                    return member;
                }
                return null;    
            }
        }

        public IEnumerable<IProjectFile> GetProjectFiles()
        {
            var declaredType = GetTypeElement();
            if (declaredType != null)
            {
                var result = declaredType.GetSourceFiles().SelectNotNull(sf => sf.ToProjectFile()).ToList();
                if (result.Count == 1)
                    return result;
            }

            var declaredElement = GetDeclaredElement();
            if (declaredElement != null)
                return declaredElement.GetSourceFiles().SelectNotNull(sf => sf.ToProjectFile());
            return EmptyList<IProjectFile>.InstanceList;
        }

        public IUnitTestRunStrategy GetRunStrategy(IHostProvider hostProvider)
        {
            return myUnityNUnitServiceProvider.GetRunStrategy(this);
        }

        public IList<UnitTestTask> GetTaskSequence(ICollection<IUnitTestElement> explicitElements, IUnitTestRun run)
        {
            var unitTestTasks = new List<UnitTestTask> {new UnitTestTask(this, new StubRemoteTask("stub"))};
            return unitTestTasks;
        }

        public UnitTestElementId Id
        {
            get { return myId; }
        }

        public string Kind
        {
            get { return "Unity Test"; }
        }

        public ISet<UnitTestElementCategory> OwnCategories
        {
            get { return myCategories; }
            set { myCategories = value; }
        }
        
        public string ExplicitReason
        {
            get { return myExplicitReason; }
            set { myExplicitReason = value; }
        }
        
        public IUnitTestElement Parent { get; set; }

        public ICollection<IUnitTestElement> Children
        {
            get { return EmptyList<IUnitTestElement>.InstanceList; }
        }

        public string ShortName
        {
            get { return myMethodName; }
        }

        public bool Explicit
        {
            get
            {
                return myExplicitReason != null;
            }
        }

        public UnitTestElementState State { get; set; }
        
        private class StubRemoteTask : RemoteTask
        {
            public StubRemoteTask(XmlElement element)
                : base(element)
            {
            }

            public StubRemoteTask(string runnerID)
                : base(runnerID)
            {
            }


            public override bool Equals(RemoteTask other)
            {
                return other is StubRemoteTask;
            }

            public override bool Equals(object obj)
            {
                return obj is StubRemoteTask;
            }

            public override int GetHashCode()
            {
                return 239;
            }

            public override bool IsMeaningfulTask
            {
                get { return true; }
            }
        }
    }
}