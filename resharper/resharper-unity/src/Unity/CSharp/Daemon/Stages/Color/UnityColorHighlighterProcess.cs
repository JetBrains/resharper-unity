using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.ColorHints;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Media;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color
{
    [HighlightingSource(HighlightingTypes = [typeof(ColorHintHighlighting)])]
    public class UnityColorHighlighterProcess : CSharpIncrementalDaemonStageProcessBase
    {
        private readonly IEnumerable<IUnityColorReferenceProvider> myProviders;
        private readonly IContextBoundSettingsStore mySettingsStore;

        public UnityColorHighlighterProcess(IEnumerable<IUnityColorReferenceProvider> providers, IDaemonProcess process, IContextBoundSettingsStore settingsStore,
                                            ICSharpFile file)
            : base(process, settingsStore, file)
        {
            myProviders = providers;
            mySettingsStore = settingsStore;
        }

        public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is ITokenNode tokenNode && tokenNode.GetTokenType().IsWhitespace) return;

            var colorInfo = CreateColorHighlightingInfo(element, myProviders);
            if (colorInfo != null)
                consumer.AddHighlighting(colorInfo.Highlighting, colorInfo.Range);
        }

        private HighlightingInfo? CreateColorHighlightingInfo(ITreeNode element, IEnumerable<IUnityColorReferenceProvider> providers)
        {
            var colorReference = GetColorReference(element, providers, mySettingsStore);
            var range = colorReference?.ColorConstantRange;
            return range?.IsValid() == true
                ? new HighlightingInfo(range.Value, new ColorHintHighlighting(colorReference))
                : null;
        }

        private static IColorReference? GetColorReference(ITreeNode element, IEnumerable<IUnityColorReferenceProvider> providers, IContextBoundSettingsStore settingsStore)
        {
            if (element is IObjectCreationExpression constructorExpression)
                return ReferenceFromConstructor(constructorExpression, settingsStore);

            var referenceExpression = element as IReferenceExpression;
            if (referenceExpression?.QualifierExpression is IReferenceExpression qualifier)
            {
                var result = ReferenceFromInvocation(qualifier, referenceExpression, settingsStore)
                    ?? ReferenceFromProperty(qualifier, referenceExpression, settingsStore);

                if (result != null)
                    return result;
            }

            foreach (var provider in providers)
            {
                var result = provider.GetColorReference(element);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static IColorReference? ReferenceFromConstructor(IObjectCreationExpression constructorExpression, IContextBoundSettingsStore settingsStore)
        {
            // Get the type from the constructor, which allows us to support target typed new. This will fail to resolve
            // if the parameters don't match (e.g. calling new Color32(r, g, b) without passing a), so fall back to the
            // expression's type, if available.
            // Note that we don't do further validation of the parameters, so we'll still show a colour preview for
            // Color32(r, g, b) even though it's an invalid method call.
            var constructedType =
                (constructorExpression.ConstructorReference.Resolve().DeclaredElement as IConstructor)?.ContainingType
                ?? constructorExpression.TypeReference?.Resolve().DeclaredElement as ITypeElement;
            if (constructedType == null)
                return null;

            var unityColorTypes = UnityColorTypes.GetInstance(constructedType.Module);
            if (!unityColorTypes.IsUnityColorType(constructedType)) return null;

            var arguments = constructorExpression.Arguments;
            if (arguments.Count is < 3 or > 4) return null;

            JetRgbaColor? color = null;
            if (unityColorTypes.UnityColorType != null && unityColorTypes.UnityColorType.Equals(constructedType))
            {
                var baseColor = GetColorFromFloatArgb(arguments);
                if (baseColor == null) return null;

                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)(255.0 * a)) : rgb;
            }
            else if (unityColorTypes.UnityColor32Type != null && unityColorTypes.UnityColor32Type.Equals(constructedType))
            {
                var baseColor = GetColorFromIntArgb(arguments);
                if (baseColor == null) return null;

                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)a) : rgb;
            }

            if (color == null) return null;

            var colorElement = new ColorElement(color.Value);
            var argumentList = constructorExpression.ArgumentList;
            return new UnityColorReference(colorElement, constructorExpression, argumentList, argumentList.GetDocumentRange(), settingsStore);
        }

        private static IColorReference? ReferenceFromInvocation(IReferenceExpression qualifier,
                                                                IReferenceExpression methodReferenceExpression, IContextBoundSettingsStore settingsStore)
        {
            var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(methodReferenceExpression);
            if (invocationExpression == null || invocationExpression.Arguments.IsEmpty)
                return null;

            var methodReference = methodReferenceExpression.Reference;

            var name = methodReference.GetName();
            if (!string.Equals(name, "HSVToRGB", StringComparison.Ordinal)) return null;

            var arguments = invocationExpression.Arguments;
            if (arguments.Count is < 3 or > 4) return null;

            var color = GetColorFromHSV(arguments);
            if (color == null) return null;

            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;

            var unityColorTypes = UnityColorTypes.GetInstance(qualifierType.Module);
            if (!unityColorTypes.IsUnityColorTypeSupportingHSV(qualifierType)) return null;

            var colorElement = new ColorElement(color.Value);
            var argumentList = invocationExpression.ArgumentList;
            return new UnityColorReference(colorElement, invocationExpression,
                argumentList, argumentList.GetDocumentRange(), settingsStore);
        }

        private static IColorReference? ReferenceFromProperty(IReferenceExpression qualifier,
                                                              IReferenceExpression colorQualifiedMemberExpression, IContextBoundSettingsStore settingsStore)
        {
            var name = colorQualifiedMemberExpression.Reference.GetName();

            var color = UnityNamedColors.Get(name);
            if (color == null) return null;

            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;

            var unityColorTypes = UnityColorTypes.GetInstance(qualifierType.Module);
            if (!unityColorTypes.IsUnityColorTypeSupportingProperties(qualifierType)) return null;

            var property = colorQualifiedMemberExpression.Reference.Resolve().DeclaredElement as IProperty;
            if (property == null) return null;

            var colorElement = new ColorElement(color.Value, name);
            return new UnityColorReference(colorElement, colorQualifiedMemberExpression,
                 colorQualifiedMemberExpression, colorQualifiedMemberExpression.NameIdentifier.GetDocumentRange(), settingsStore);
        }

        private static (float? alpha, JetRgbaColor)? GetColorFromFloatArgb(ICollection<ICSharpArgument> arguments)
        {
            var a = GetArgumentAsFloatConstant(arguments, "a", 0, 1);
            var r = GetArgumentAsFloatConstant(arguments, "r", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "g", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "b", 0, 1);

            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)(255.0 * r.Value), (byte)(255.0 * g.Value), (byte)(255.0 * b.Value)));
        }

        private static (int? alpha, JetRgbaColor)? GetColorFromIntArgb(ICollection<ICSharpArgument> arguments)
        {
            var a = GetArgumentAsIntConstant(arguments, "a", 0, 255);
            var r = GetArgumentAsIntConstant(arguments, "r", 0, 255);
            var g = GetArgumentAsIntConstant(arguments, "g", 0, 255);
            var b = GetArgumentAsIntConstant(arguments, "b", 0, 255);

            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)r.Value, (byte)g.Value, (byte)b.Value));
        }

        private static JetRgbaColor? GetColorFromHSV(ICollection<ICSharpArgument> arguments)
        {
            var h = GetArgumentAsFloatConstant(arguments, "H", 0, 1);
            var s = GetArgumentAsFloatConstant(arguments, "S", 0, 1);
            var v = GetArgumentAsFloatConstant(arguments, "V", 0, 1);

            if (!h.HasValue || !s.HasValue || !v.HasValue) return null;

            return ColorUtils.ColorFromHSV(h.Value, s.Value, v.Value);
        }

        private static float? GetArgumentAsFloatConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            float min, float max)
        {
            var expression = GetNamedArgument(arguments, parameterName)?.Expression;
            return ArgumentAsFloatConstant(min, max, expression);
        }

        public static float? ArgumentAsFloatConstant(float min, float max, IExpression? expression)
        {
            if (expression == null) return null;

            double? value = null;
            if (expression.ConstantValue.IsDouble())
                value = expression.ConstantValue.DoubleValue;
            else if (expression.ConstantValue.IsFloat())
                value = expression.ConstantValue.FloatValue;
            else if (expression.ConstantValue.IsInteger())
                value = expression.ConstantValue.IntValue;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == null || value.Value.IsNanOrInf() || value.Value.Clamp(min, max) != value.Value)
                return null;

            return (float) value.Value;
        }

        private static int? GetArgumentAsIntConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            int min, int max)
        {
            // TODO: Use conditional access when the monorepo build uses a more modern C# compiler
            // Currently (as of 01/2023) the monorepo build for Unity uses C#9 compiler, which will complain that the
            // out variable is uninitialised when we use conditional access
            // See also https://youtrack.jetbrains.com/issue/RSRP-489147
            var constantValue = GetNamedArgument(arguments, parameterName)?.Expression?.ConstantValue;
            return constantValue != null && constantValue.IsInteger(out var value) && value.Clamp(min, max) == value
                ? value
                : null;
        }

        private static ICSharpArgument? GetNamedArgument(IEnumerable<ICSharpArgument> arguments, string parameterName)
        {
            return arguments.FirstOrDefault(a =>
                parameterName.Equals(a.MatchingParameter?.Element.ShortName, StringComparison.Ordinal));
        }
    }
}
