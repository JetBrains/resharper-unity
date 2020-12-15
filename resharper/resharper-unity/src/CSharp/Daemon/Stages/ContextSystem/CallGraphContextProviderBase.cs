using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public abstract class CallGraphContextProviderBase : ICallGraphContextProvider
    {
        private readonly IElementIdProvider myElementIdProvider;
        private readonly CallGraphSwaExtensionProvider myCallGraphSwaExtensionProvider;
        private readonly CallGraphCommentMarksProvider myMarksProviderBase;
        private readonly SolutionAnalysisService myService;

        protected CallGraphContextProviderBase(
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            CallGraphCommentMarksProvider marksProviderBase,
            SolutionAnalysisService service)
        {
            myElementIdProvider = elementIdProvider;
            myCallGraphSwaExtensionProvider = callGraphSwaExtensionProvider;
            myMarksProviderBase = marksProviderBase;
            myService = service;
        }

        public abstract CallGraphContextElement Context { get; }
        public abstract bool IsContextAvailable { get; }
        public virtual bool IsContextChangingNode([NotNull] ITreeNode node) => UnityCallGraphUtil.IsFunctionNode(node);
        protected virtual bool DeclarationChecksDeclaredElement { get; } = true;
        protected virtual bool ExpressionChecksDeclaredElement { get; } = false;

        #region checks

        protected virtual bool CheckDeclaration([NotNull] IDeclaration declaration, out bool isMarked)
        {
            if (myMarksProviderBase.HasMarkComment(declaration, out isMarked))
                return isMarked;

            if (DeclarationChecksDeclaredElement)
            {
                var declaredElement = declaration.DeclaredElement;

                if (declaredElement != null && CheckDeclaredElement(declaredElement, out isMarked))
                    return true;
            }

            isMarked = false;
            return false;
        }

        protected virtual bool CheckExpression([NotNull] ICSharpExpression expression, out bool isMarked)
        {
            if (ExpressionChecksDeclaredElement)
            {
                var callee = CallGraphUtil.GetCallee(expression);

                if (callee != null && CheckDeclaredElement(callee, out isMarked))
                    return true;
            }

            isMarked = false;
            return false;
        }

        protected virtual bool CheckDeclaredElement([NotNull] IDeclaredElement element, out bool isMarked)
        {
            isMarked = false;
            return false;
        }

        #endregion

        #region internal

        private bool IsMarkedStageInternal([CanBeNull] object obj, DaemonProcessKind processKind)
        {
            var shouldPropagate = processKind == DaemonProcessKind.GLOBAL_WARNINGS;

            return IsMarkedInternal(obj, shouldPropagate, shouldSync: false);
        }

        private bool IsMarkedSweaInternal([CanBeNull] object obj)
        {
            var shouldPropagate = UnityCallGraphUtil.IsSweaCompleted(myService);

            return IsMarkedInternal(obj, shouldPropagate, shouldSync: false);
        }

        private bool IsMarkedSyncInternal([CanBeNull] object obj) => IsMarkedInternal(obj, shouldPropagate: true, shouldSync: true);

        private bool IsMarkedInternal([CanBeNull] object obj, bool shouldPropagate, bool shouldSync)
        {
            if (IsContextAvailable == false)
                return false;

            var query = GetQuery(obj);

            if (query.Check(out var isMarked))
                return isMarked;

            if (!shouldPropagate)
                return false;

            var declaredElement = query.ExtractElement();

            if (declaredElement == null)
                return false;

            var elementId = myElementIdProvider.GetElementId(declaredElement);

            if (!elementId.HasValue)
                return false;

            var elementIdValue = elementId.Value;
            var markId = myMarksProviderBase.Id;

            return shouldSync
                ? myCallGraphSwaExtensionProvider.IsMarkedSync(markId, elementIdValue)
                : myCallGraphSwaExtensionProvider.IsMarked(markId, elementIdValue);
        }

        #endregion

        #region interface

        public bool IsMarkedStage(IDeclaration declaration, DaemonProcessKind processKind) =>
            IsMarkedStageInternal(declaration, processKind);

        public bool IsMarkedStage(ICSharpExpression expression, DaemonProcessKind processKind) =>
            IsMarkedStageInternal(expression, processKind);

        public bool IsMarkedStage(IDeclaredElement declaredElement, DaemonProcessKind processKind) =>
            IsMarkedStageInternal(declaredElement, processKind);

        public bool IsMarkedSync(IDeclaration declaration) => IsMarkedSyncInternal(declaration);
        public bool IsMarkedSync(ICSharpExpression expression) => IsMarkedSyncInternal(expression);
        public bool IsMarkedSync(IDeclaredElement declaredElement) => IsMarkedSyncInternal(declaredElement);

        public bool IsMarkedSwea(IDeclaration declaration) => IsMarkedSweaInternal(declaration);
        public bool IsMarkedSwea(ICSharpExpression expression) => IsMarkedSweaInternal(expression);
        public bool IsMarkedSwea(IDeclaredElement declaredElement) => IsMarkedSweaInternal(declaredElement);

        #endregion

        #region query

        private IQuery GetQuery([CanBeNull] object obj)
        {
            switch (obj)
            {
                case IDeclaration declaration:
                    return new DeclarationQuery(declaration, this);
                case ICSharpExpression cSharpExpression:
                    return new CSharpExpressionQuery(cSharpExpression, this);
                case IDeclaredElement declaredElement:
                    return new DeclaredElementQuery(declaredElement, this);
                case null:
                    return new NullQuery();
            }

            throw new ArgumentException("object type is" + obj.GetType().Name);
        }

        private interface IQuery
        {
            [CanBeNull]
            IDeclaredElement ExtractElement();

            bool Check(out bool isMarked);
        }

        private sealed class NullQuery : IQuery
        {
            public IDeclaredElement ExtractElement()
            {
                return null;
            }

            public bool Check(out bool isMarked)
            {
                isMarked = false;
                return false;
            }
        }

        private sealed class DeclarationQuery : IQuery
        {
            [NotNull] private readonly IDeclaration myDeclaration;
            [NotNull] private readonly CallGraphContextProviderBase myContextProvider;

            public DeclarationQuery([NotNull] IDeclaration declaration, [NotNull] CallGraphContextProviderBase contextProvider)
            {
                myDeclaration = declaration;
                myContextProvider = contextProvider;
            }

            public IDeclaredElement ExtractElement()
            {
                return myDeclaration.DeclaredElement;
            }

            public bool Check(out bool isMarked)
            {
                return myContextProvider.CheckDeclaration(myDeclaration, out isMarked);
            }
        }

        private sealed class DeclaredElementQuery : IQuery
        {
            [NotNull] private readonly IDeclaredElement myDeclaredElement;
            [NotNull] private readonly CallGraphContextProviderBase myContextProvider;

            public DeclaredElementQuery([NotNull] IDeclaredElement declaredElement, [NotNull] CallGraphContextProviderBase contextProvider)
            {
                myDeclaredElement = declaredElement;
                myContextProvider = contextProvider;
            }

            public IDeclaredElement ExtractElement()
            {
                return myDeclaredElement;
            }

            public bool Check(out bool isMarked)
            {
                return myContextProvider.CheckDeclaredElement(myDeclaredElement, out isMarked);
            }
        }

        private sealed class CSharpExpressionQuery : IQuery
        {
            [NotNull] private readonly ICSharpExpression myCSharpExpression;
            [NotNull] private readonly CallGraphContextProviderBase myContextProvider;

            public CSharpExpressionQuery([NotNull] ICSharpExpression cSharpExpression,
                [NotNull] CallGraphContextProviderBase contextProvider)
            {
                myCSharpExpression = cSharpExpression;
                myContextProvider = contextProvider;
            }

            public IDeclaredElement ExtractElement()
            {
                return CallGraphUtil.GetCallee(myCSharpExpression);
            }

            public bool Check(out bool isMarked)
            {
                return myContextProvider.CheckExpression(myCSharpExpression, out isMarked);
            }
        }

        #endregion
    }
}