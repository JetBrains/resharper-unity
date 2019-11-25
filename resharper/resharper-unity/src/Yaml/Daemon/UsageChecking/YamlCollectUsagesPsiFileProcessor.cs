using System;
using JetBrains.Collections;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking
{
    public class YamlCollectUsagesPsiFileProcessor : ICollectUsagesPsiFileProcessor, IRecursiveElementProcessor<IScopeProcessor>
    {
        private readonly IDaemonProcess myDaemonProcess;

        public YamlCollectUsagesPsiFileProcessor(IDaemonProcess daemonProcess)
        {
            myDaemonProcess = daemonProcess;
        }

        public void ProcessFile(IDaemonProcess daemonProcess, IFile psiFile, IScopeProcessor topLevelScopeProcessor)
        {
            psiFile.ProcessThisAndDescendants(this, topLevelScopeProcessor);
        }

        public bool InteriorShouldBeProcessed(ITreeNode element, IScopeProcessor context)
        {

            if (element is IChameleonNode)
                return UnityYamlReferenceUtil.CanContainReference(element.GetTextAsBuffer());

            return true;
        }

        public bool IsProcessingFinished(IScopeProcessor context)
        {
            if (myDaemonProcess.InterruptFlag)
                throw new OperationCanceledException();
            return false;
        }

        public void ProcessBeforeInterior(ITreeNode element, IScopeProcessor context)
        {
            var usageData = context.UsageData;
            foreach (var reference in element.GetReferences())
            {
                var declaredElement = reference.Resolve().DeclaredElement;
                if (declaredElement == null)
                    continue;

                usageData.SetElementState(declaredElement, UsageState.USED_MASK);
                usageData.AddUsage(declaredElement, ReferenceCounter.Id);
                usageData.AddUsage(declaredElement, UnityEditorUsageCounter.Id);

                if (declaredElement is IAccessor accessor && accessor.OwnerMember is IProperty property)
                {
                    usageData.SetElementState(property, UsageState.NAME_CAPTURED | UsageState.ASSIGNED);
                    usageData.AddUsage(property, ReferenceCounter.Id);
                    usageData.AddUsage(property, UnityEditorUsageCounter.Id);
                }
            }

            foreach (var (_, swaElement) in context.UsageData.SwaExtensionsData)
            {
                swaElement.ProcessBeforeInterior(element, context);
                swaElement.ProcessNode(element, context);
            }
        }

        public void ProcessAfterInterior(ITreeNode element, IScopeProcessor context)
        {
            foreach (var (_, swaElement) in context.UsageData.SwaExtensionsData)
            {
                swaElement.ProcessAfterInterior(element, context);
            }
        }
    }
}