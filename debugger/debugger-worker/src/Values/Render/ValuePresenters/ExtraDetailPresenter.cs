using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.Render.ValuePresenters;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ValuePresenters
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ExtraDetailPresenter<TValue> : ValuePresenterBase<TValue, IValueRole<TValue>>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;

        public ExtraDetailPresenter(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        public override int Priority => UnityRendererUtil.ValuePresenterPriority;

        public override bool IsApplicable(IValueRole<TValue> role,
                                          IMetadataTypeLite instanceType,
                                          IPresentationOptions options,
                                          IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && role.ValueReference is ExtraDetailValueReferenceDecorator<TValue>;
        }

        public override IValuePresentation PresentValue(IValueRole<TValue> valueRole,
                                                        IMetadataTypeLite instanceType,
                                                        IPresentationOptions options,
                                                        IUserDataHolder dataHolder,
                                                        CancellationToken token)
        {
            var extraDetail = (ExtraDetailValueReferenceDecorator<TValue>) valueRole.ValueReference;
            var presentation = extraDetail.UnderlyingValueReference.ToValue(ValueServices).GetValuePresentation(options, token);
            if (presentation.ResultKind == ValueResultKind.Success)
            {
                var presentationBuilder = PresentationBuilder.New(presentation.Value.ToArray())
                    .Add(ValuePresentationPart.Space)
                    .SpecialSymbol("(").Default(extraDetail.ExtraDetail).SpecialSymbol(")");
                return SimplePresentation.CreateSuccess(presentationBuilder.Result(), presentation.Flags,
                    presentation.Type, presentation.PrimitiveValue);
            }

            return presentation;
        }
    }
}