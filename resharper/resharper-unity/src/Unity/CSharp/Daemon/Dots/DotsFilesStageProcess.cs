using System;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Dots
{
    internal class DotsFilesStageProcess : IDaemonStageProcess, IRecursiveElementProcessor
    {
        private readonly CollectUsagesStageProcess myTypeUsageProcess;
        private readonly IFile myFile;

        public DotsFilesStageProcess(CollectUsagesStageProcess typeUsageProcess, IDaemonProcess process, IFile file)
        {
            myTypeUsageProcess = typeUsageProcess;
            DaemonProcess = process;
            myFile = file;
        }

        public IDaemonProcess DaemonProcess { get; }

        bool IRecursiveElementProcessor.ProcessingIsFinished => DaemonProcess.InterruptFlag;

        void IDaemonStageProcess.Execute(Action<DaemonStageResult> committer)
        {
            committer(InternalExecute());
        }

        private DaemonStageResult InternalExecute()
        {
            myFile?.ProcessDescendants(this);
            return null;
        }

        bool IRecursiveElementProcessor.InteriorShouldBeProcessed(ITreeNode element)
        {
            if (element is ITypeAndNamespaceHolderDeclaration or INamespaceBody or IClassBody)
                return true;

            if (element is not IClassLikeDeclaration classLikeDeclaration) 
                return false;
            
            var typeElement = classLikeDeclaration.DeclaredElement;
            if (typeElement == null)
                return false;

            if (!typeElement.DerivesFrom(KnownTypes.ISystem) && !typeElement.DerivesFrom(KnownTypes.ComponentSystemBase))
                return true;
            
            foreach (var methodDeclaration in classLikeDeclaration.MethodDeclarationsEnumerable)
            {
                if (DaemonProcess.InterruptFlag) 
                    break;
                
                var method = methodDeclaration.DeclaredElement;
                if ( method is { IsStatic: false })
                    myTypeUsageProcess.SetElementState(method, UsageState.CANNOT_BE_STATIC);
            }

            return true;
        }

        void IRecursiveElementProcessor.ProcessBeforeInterior(ITreeNode element)
        {
        }

        void IRecursiveElementProcessor.ProcessAfterInterior(ITreeNode element)
        {
        }
    }
}