using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Resolve
{
    // from inputactions to csharp
    public class InputActionsNameReferenceFactory : IReferenceFactory
    {
        private readonly IPsiSourceFile mySourceFile;

        public InputActionsNameReferenceFactory(IPsiSourceFile sourceFile)
        {
            mySourceFile = sourceFile;
        }

        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<UnityInputActionsReference>(oldReferences, element))
                return oldReferences;
            
            // return IsActionName(element)
            //     ? new ReferenceCollection(new InputActionsNameReference())
            //     : ReferenceCollection.Empty;
            return ReferenceCollection.Empty;
        }

        private bool IsActionName(ITreeNode? node)
        {
            if (node is not IJsonNewLiteralExpression) return false;
            return mySourceFile.GetSolution().GetComponent<InputActionsCache>().ContainsOffset(mySourceFile, node);
        }

        public bool HasReference(ITreeNode node, IReferenceNameContainer names)
        {
            return mySourceFile.GetSolution().GetComponent<InputActionsCache>().ContainsOffset(mySourceFile, node);
        }
    }
}