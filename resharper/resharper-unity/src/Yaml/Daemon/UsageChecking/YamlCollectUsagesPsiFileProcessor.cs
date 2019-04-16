using System;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking
{
    public class YamlCollectUsagesPsiFileProcessor : ICollectUsagesPsiFileProcessor, IRecursiveElementProcessor<UsageData>
    {
        private readonly IDaemonProcess myDaemonProcess;

        public YamlCollectUsagesPsiFileProcessor(IDaemonProcess daemonProcess)
        {
            myDaemonProcess = daemonProcess;
        }

        public void ProcessFile(IDaemonProcess daemonProcess, IFile psiFile, IScopeProcessor topLevelScopeProcessor)
        {
            psiFile.ProcessThisAndDescendants(this, topLevelScopeProcessor.UsageData);
        }

        public bool InteriorShouldBeProcessed(ITreeNode element, UsageData context)
        {
            if (element is IYamlDocument document)
                return UnityYamlReferenceUtil.CanContainReference(document);

            return true;
        }

        public bool IsProcessingFinished(UsageData context)
        {
            if (myDaemonProcess.InterruptFlag)
                throw new OperationCanceledException();
            return false;
        }

        public void ProcessBeforeInterior(ITreeNode element, UsageData context)
        {
            foreach (var reference in element.GetReferences())
            {
                var declaredElement = reference.Resolve().DeclaredElement;
                if (declaredElement == null)
                    continue;

                context.SetElementState(declaredElement, UsageState.USED_MASK);
                context.AddUsage(declaredElement, UsageCounterKind.Reference);

                if (declaredElement is IAccessor accessor && accessor.OwnerMember is IProperty property)
                {
                    context.SetElementState(property, UsageState.NAME_CAPTURED | UsageState.ASSIGNED);
                    context.AddUsage(property, UsageCounterKind.Reference);
                }
            }
        }

        public void ProcessAfterInterior(ITreeNode element, UsageData context)
        {
        }
    }
}