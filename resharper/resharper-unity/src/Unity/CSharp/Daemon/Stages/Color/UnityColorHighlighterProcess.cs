using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Media;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color
{
    public class UnityColorHighlighterProcess : CSharpIncrementalDaemonStageProcessBase
    {
        public UnityColorHighlighterProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore,
                                            ICSharpFile file)
            : base(process, settingsStore, file)
        {
        }

        public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
        {
            if (element is ITokenNode tokenNode && tokenNode.GetTokenType().IsWhitespace) return;

            var colorInfo = CreateColorHighlightingInfo(element);
            if (colorInfo != null)
                consumer.AddHighlighting(colorInfo.Highlighting, colorInfo.Range);
        }

        private HighlightingInfo? CreateColorHighlightingInfo(ITreeNode element)
        {
            var colorReference = GetColorReference(element);
            var range = colorReference?.ColorConstantRange;
            return range?.IsValid() == true
                ? new HighlightingInfo(range.Value, new ColorHighlighting(colorReference))
                : null;
        }

        private static IColorReference? GetColorReference(ITreeNode element)
        {
            if (element is IObjectCreationExpression constructorExpression)
                return ReferenceFromConstructor(constructorExpression);

            var referenceExpression = element as IReferenceExpression;
            if (referenceExpression?.QualifierExpression is not IReferenceExpression qualifier)
                return null;

            return ReferenceFromInvocation(qualifier, referenceExpression)
                   ?? ReferenceFromProperty(qualifier, referenceExpression);
        }

        private static IColorReference? ReferenceFromConstructor(IObjectCreationExpression constructorExpression)
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
                var baseColor = GetColorFromFloatARGB(arguments);
                if (baseColor == null) return null;

                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)(255.0 * a)) : rgb;
            }
            else if (unityColorTypes.UnityColor32Type != null && unityColorTypes.UnityColor32Type.Equals(constructedType))
            {
                var baseColor = GetColorFromIntARGB(arguments);
                if (baseColor == null) return null;

                var (a, rgb) = baseColor.Value;
                color = a.HasValue ? rgb.WithA((byte)a) : rgb;
            }

            if (color == null) return null;

            var colorElement = new ColorElement(color.Value);
            var argumentList = constructorExpression.ArgumentList;
            return new UnityColorReference(colorElement, constructorExpression, argumentList, argumentList.GetDocumentRange());
        }

        private static IColorReference? ReferenceFromInvocation(IReferenceExpression qualifier,
                                                                IReferenceExpression methodReferenceExpression)
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
                argumentList, argumentList.GetDocumentRange());
        }

        private static IColorReference? ReferenceFromProperty(IReferenceExpression qualifier,
                                                              IReferenceExpression colorQualifiedMemberExpression)
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
                 colorQualifiedMemberExpression, colorQualifiedMemberExpression.NameIdentifier.GetDocumentRange());
        }

        private static (float? alpha, JetRgbaColor)? GetColorFromFloatARGB(ICollection<ICSharpArgument> arguments)
        {
            var a = GetArgumentAsFloatConstant(arguments, "a", 0, 1);
            var r = GetArgumentAsFloatConstant(arguments, "r", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "g", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "b", 0, 1);

            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return (a, JetRgbaColor.FromRgb((byte)(255.0 * r.Value), (byte)(255.0 * g.Value), (byte)(255.0 * b.Value)));
        }

        private static (int? alpha, JetRgbaColor)? GetColorFromIntARGB(ICollection<ICSharpArgument> arguments)
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
            var namedArgument = GetNamedArgument(arguments, parameterName);
            if (namedArgument == null) return null;

            var matchingParameter = namedArgument.MatchingParameter.NotNull("matchingParameter != null");
            var paramType = matchingParameter.Element.Type;

            var expression = namedArgument.Expression;
            if (expression == null) return null;

            var constantValue = expression.ConstantValue;
            if (constantValue.IsErrorOrNonCompileTimeConstantValue()) return null;

            var conversionRule = namedArgument.GetTypeConversionRule();
            if (!expression.GetExpressionType().IsImplicitlyConvertibleTo(paramType, conversionRule))
            {
                return null;
            }

            double? value = null;
            try
            {
                value = Convert.ToDouble(constantValue.Value, CultureInfo.InvariantCulture);
            }
            catch
            {
                // ignored
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value == null || value.Value.IsNanOrInf() || value.Value.Clamp(min, max) != value.Value)
                return null;

            return (float) value.Value;
        }

        private static int? GetArgumentAsIntConstant(IEnumerable<ICSharpArgument> arguments, string parameterName,
            int min, int max)
        {
            var namedArgument = GetNamedArgument(arguments, parameterName);
            if (namedArgument == null) return null;

            var matchingParameter = namedArgument.MatchingParameter.NotNull("matchingParameter != null");
            var paramType = matchingParameter.Element.Type;

            var expression = namedArgument.Expression;
            if (expression == null) return null;

            var constantValue = expression.ConstantValue;
            if (constantValue.IsErrorOrNonCompileTimeConstantValue()) return null;

            var conversionRule = namedArgument.GetTypeConversionRule();
            if (!expression.GetExpressionType().IsImplicitlyConvertibleTo(paramType, conversionRule))
            {
                return null;
            }

            int? value = null;
            try
            {
                value = Convert.ToInt32(constantValue.Value, CultureInfo.InvariantCulture);
            }
            catch
            {
                // ignored
            }

            if (value == null || value.Value.Clamp(min, max) != value.Value)
                return null;

            return value.Value;
        }

        private static ICSharpArgument? GetNamedArgument(IEnumerable<ICSharpArgument> arguments, string parameterName)
        {
            return arguments.FirstOrDefault(a =>
                parameterName.Equals(a.MatchingParameter?.Element.ShortName, StringComparison.Ordinal));
        }
    }
}