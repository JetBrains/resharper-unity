using System.Threading;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.Render.ValuePresenters;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ValuePresenters
{
    // Overrides StringValuePresenter for our "informational" values
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class TextValuePresenter<TValue> : ValuePresenterBase<TValue, IStringValueRole<TValue>>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;

        public TextValuePresenter(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        public override int Priority => UnityRendererUtil.ValuePresenterPriority;

        public override bool IsApplicable(IStringValueRole<TValue> role, IMetadataTypeLite instanceType, IPresentationOptions options,
                                          IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && role.ValueReference is NamedReferenceDecorator<TValue>;
        }

        public override IValuePresentation PresentValue(IStringValueRole<TValue> valueRole, IMetadataTypeLite instanceType,
                                                        IPresentationOptions options, IUserDataHolder dataHolder, CancellationToken token)
        {
            // Note that ValueFlags.IsString will add the "View" link to see a string in a popup. We don't need this
            var text = valueRole.GetString();
            return SimplePresentation.CreateSuccess(ValuePresentationPart.Default(text),
                valueRole.ValueReference.DefaultFlags | ValueFlags.NoChildren, instanceType, text);
        }
    }
}