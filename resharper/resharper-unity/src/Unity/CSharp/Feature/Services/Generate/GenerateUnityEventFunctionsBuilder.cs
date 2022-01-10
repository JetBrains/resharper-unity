using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate.MemberBody;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.GenerateMemberBody;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate
{
    [GeneratorBuilder(GeneratorUnityKinds.UnityEventFunctions, typeof(CSharpLanguage))]
    public class GenerateUnityEventFunctionsBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
    {
        private readonly UnityApi myUnityApi;
        private readonly IApplicationWideContextBoundSettingStore mySettingsStore;

        public GenerateUnityEventFunctionsBuilder(ISolution solution, UnityApi unityApi,
                                                  IApplicationWideContextBoundSettingStore settingsStore)
        {
            myUnityApi = unityApi;
            mySettingsStore = settingsStore;
        }

        public override double Priority => 100;

        // Enables/disables the menu item
        protected override bool IsAvailable(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.IsFromUnityProject() && HasUnityBaseType(context) && base.IsAvailable(context);
        }

        protected override void Process(CSharpGeneratorContext context, IProgressIndicator progress)
        {
            if (!HasUnityBaseType(context)) return;

            var factory = CSharpElementFactory.GetInstance(context.ClassDeclaration);
            var selectedGeneratorElements = context.InputElements.OfType<GeneratorDeclaredElement>();
            foreach (var generatorElement in selectedGeneratorElements)
            {
                if (!(generatorElement.DeclaredElement is IMethod selectedMethod)) continue;

                var methodDeclaration = (IMethodDeclaration) CSharpGenerateUtil.CreateMemberDeclaration(
                    context.ClassDeclaration, generatorElement.Substitution, selectedMethod, false, out _);

                methodDeclaration.SetAccessRights(selectedMethod.GetAccessRights());
                methodDeclaration.SetStatic(selectedMethod.IsStatic);

                IBlock block;
                if (selectedMethod.IsVirtual)
                {
                    methodDeclaration.SetOverride(true);
                    var parameters = string.Join(",", selectedMethod.Parameters.Select(p => p.ShortName));
                    block = factory.CreateBlock("{base.$0($1);}", selectedMethod.ShortName, parameters);

                }
                else
                {
                    var implementationKind = mySettingsStore.BoundSettingsStore
                        .GetValue((GenerateMemberBodySettings key) => key.MethodImplementationKind);
                    block = CreateMethodBody(methodDeclaration, implementationKind);
                }

                methodDeclaration.SetBody(block);

                methodDeclaration.FormatNode();
                context.PutMemberDeclaration(methodDeclaration);
            }
        }

        private static IBlock CreateMethodBody(ICSharpDeclaration declaration, MethodImplementationKind implementationKind)
        {
            var factory = CSharpElementFactory.GetInstance(declaration);

            switch (implementationKind)
            {
                case MethodImplementationKind.ThrowNotImplemented:
                {
                    var predefinedType = declaration.GetPredefinedType();
                    return factory.CreateBlock("{throw new $0();}", predefinedType.NotImplementedException);
                }
                case MethodImplementationKind.ReturnDefaultValue:
                {
                    return CSharpReturnStatementMemberBodyProvider.CreateBody(declaration);
                }
                case MethodImplementationKind.NotCompiledCode:
                {
                    if (declaration.DeclaredElement is IMethod method && !method.ReturnType.IsVoid())
                    {
                        return factory.CreateBlock("{ return TODO_IMPLEMENT_ME; }");
                    }

                    return factory.CreateBlock("{ TODO_IMPLEMENT_ME(); }");
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(implementationKind));
                }
            }
        }

        private bool HasUnityBaseType(CSharpGeneratorContext context)
        {
            return context.ClassDeclaration.DeclaredElement is IClass typeElement && myUnityApi.IsUnityType(typeElement);
        }
    }
}