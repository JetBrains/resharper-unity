using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.AttributeChecker;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;


namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class UnityTestFileExplorer : IRecursiveElementProcessor
    {
        private readonly IFile myFile;
        private readonly UnityTestElementFactory myFactory;
        private readonly UnitTestAttributeCache myUnitTestAttributeCache;
        private readonly IUnitTestElementsObserver myObserver;
        private readonly Func<bool> myInterrupted;
        private readonly IProject myProject;
        private readonly TargetFrameworkId myTargetFrameworkId;

        public UnityTestFileExplorer(IFile file, UnityTestElementFactory factory, UnitTestAttributeCache unitTestAttributeCache,
            IUnitTestElementsObserver observer, Func<bool> interrupted, IProject project)
        {
            myFile = file;
            myFactory = factory;
            myUnitTestAttributeCache = unitTestAttributeCache;
            myObserver = observer;
            myInterrupted = interrupted;
            myProject = project;
            myTargetFrameworkId = myProject.GetCurrentTargetFrameworkId();
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            return !(element is ITypeMemberDeclaration) || (element is ITypeDeclaration);
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        public bool ProcessingIsFinished
        {
            get
            {
                if (myInterrupted())
                    throw new OperationCanceledException();
                return false;
            }
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
            var declaration = element as IDeclaration;
            if (declaration == null)
                return;

            var navigationRange = declaration.GetNameDocumentRange().TextRange;
            var containingRange = declaration.GetDocumentRange().TextRange;

            IUnitTestElement testElement = null;

            var declaredElement = declaration.DeclaredElement;
            if (declaredElement == null || declaredElement.ShortName == SharedImplUtil.MISSING_DECLARATION_NAME)
                return;

            var typeMember = declaredElement as ITypeMember;
            if (typeMember != null && IsTestMethod(typeMember, myUnitTestAttributeCache, myProject))
            {
                var containingType = typeMember.GetContainingType();
                if (containingType != null)
                {
                    var typeName = containingType.GetClrName().GetPersistent();
                    var id = string.Format("{0}.{1}{2}", typeName,
                        !containingType.GetClrName().Equals(typeName) ? containingType.ShortName + "." : string.Empty,
                        typeMember.ShortName);

                    testElement = myFactory.GetOrCreateTest(id, myProject, myTargetFrameworkId, typeName,
                        typeMember.ShortName);
                    
                    if (navigationRange.IsValid && containingRange.IsValid)
                        myObserver.OnUnitTestElementDisposition(new UnitTestElementDisposition(testElement,
                            myFile.GetSourceFile().ToProjectFile(), navigationRange, containingRange,
                            EmptyList<IUnitTestElement>.Instance));
                }
            }
        }

        private static bool IsTestMethod(ITypeMember element, UnitTestAttributeCache attributeChecker, IProject project)
        {
            var method = element as IMethod;
            if (method == null)
                return false;

            if (method.IsAbstract || method.GetAccessRights() != AccessRights.PUBLIC)
                return false;

            return attributeChecker.HasAttributeOrDerivedAttribute(project, method,
                UnityTestProvider.UnityTestAttribute);
        }
    }
}