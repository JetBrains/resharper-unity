using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.
    AddDiscardAttribute
{
    public sealed class AddDiscardAttributeBulbAction : IBulbAction
    {
        private AddDiscardAttributeBulbAction([NotNull] IMethodDeclaration methodDeclaration)
        {
            myMethodDeclaration = methodDeclaration;
        }

        [NotNull] private readonly IMethodDeclaration myMethodDeclaration;

        public void Execute(ISolution solution, ITextControl textControl)
        {
            CallGraphActionUtil.AppendAttributeInTransaction(
                myMethodDeclaration, Array.Empty<AttributeValue>(),
                Array.Empty<Pair<string, AttributeValue>>(),
                KnownTypes.BurstDiscardAttribute, GetType().Name);
        }

        public string Text => AddDiscardAttributeUtil.DiscardActionMessage;

        [ContractAnnotation("null => null")]
        [ContractAnnotation("notnull => notnull")]
        public static AddDiscardAttributeBulbAction Create([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            return methodDeclaration == null
                ? null
                : new AddDiscardAttributeBulbAction(methodDeclaration);
        }
    }
}