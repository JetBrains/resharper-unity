using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.Render.ValuePresenters;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ValuePresenters
{
    // Overrides StringValuePresenter for our "informational" values, without normal string highlighting/quote handling
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
            return myUnityOptions.ExtensionsEnabled && role.ValueReference is TextValueReference<TValue>;
        }

        public override IValuePresentation PresentValue(IStringValueRole<TValue> valueRole, IMetadataTypeLite instanceType,
                                                        IPresentationOptions options, IUserDataHolder dataHolder, CancellationToken token)
        {
            // Present the value's string as plain text, without any syntax highlighting or quote handling. Don't use
            // ValueFlags.IsString, as it will add an unnecessary "View" link - our text is always short
            var text = valueRole.GetString();
            return SimplePresentation.CreateSuccess(ValuePresentationPart.Default(text),
                valueRole.ValueReference.DefaultFlags | ValueFlags.NoChildren, instanceType, text);
        }
    }
}