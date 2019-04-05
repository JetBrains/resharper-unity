using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Daemon.UsageChecking
{
    public class YamlCollectUsagesPsiFileProcessor : ICollectUsagesPsiFileProcessor, IRecursiveElementProcessor<UsageData>
    {
        public void ProcessFile(IDaemonProcess daemonProcess, IFile psiFile, IScopeProcessor topLevelScopeProcessor)
        {
            psiFile.ProcessThisAndDescendants(this, topLevelScopeProcessor.UsageData);
        }

        public bool InteriorShouldBeProcessed(ITreeNode element, UsageData context)
        {
            // TODO [Matt Ellis] Do not open chameleon blocks
            return true;
        }

        public bool IsProcessingFinished(UsageData context)
        {
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
            }
        }

        public void ProcessAfterInterior(ITreeNode element, UsageData context)
        {
        }
    }
}