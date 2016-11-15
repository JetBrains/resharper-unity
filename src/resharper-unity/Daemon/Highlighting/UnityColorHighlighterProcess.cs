using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Psi.Colors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Special;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Highlighting
{
    [DaemonStage(StagesBefore = new[] {typeof(IdentifierHighlightingStage)})]
    public class UnityColorHighlightingStage : CSharpDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
            DaemonProcessKind processKind, ICSharpFile file)
        {
            if (processKind == DaemonProcessKind.VISIBLE_DOCUMENT &&
                settings.GetValue(HighlightingSettingsAccessor.ColorUsageHighlightingEnabled))
            {
                return new UnityColorHighlighterProcess(process, settings, file);
            }
            return null;
        }

        protected override bool IsSupported(IPsiSourceFile sourceFile)
        {
            if (sourceFile == null || !sourceFile.IsValid())
                return false;

            return sourceFile.IsLanguageSupported<CSharpLanguage>();
        }
    }

    public class UnityColorHighlighterProcess : CSharpIncrementalDaemonStageProcessBase
    {
        public UnityColorHighlighterProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore,
            ICSharpFile file)
            : base(process, settingsStore, file)
        {
        }

        public override void VisitNode(ITreeNode element, IHighlightingConsumer consumer)
        {
            var tokenNode = element as ITokenNode;
            if (tokenNode != null && tokenNode.GetTokenType().IsWhitespace) return;

            var colorInfo = CreateColorHighlightingInfo(element);
            if (colorInfo != null)
                consumer.AddHighlighting(colorInfo.Highlighting, colorInfo.Range);
        }

        private HighlightingInfo CreateColorHighlightingInfo(ITreeNode element)
        {
            var colorReference = GetColorReference(element);
            var constantRange = colorReference?.ColorConstantRange;
            if (constantRange == null) return null;

            var documentRange = constantRange.Value;
            if (!documentRange.IsValid()) return null;

            return new HighlightingInfo(documentRange, new ColorHighlighting(colorReference));
        }

        private IColorReference GetColorReference(ITreeNode element)
        {
            var constructorExpression = element as IObjectCreationExpression;
            if (constructorExpression != null)
                return ReferenceFromConstructor(constructorExpression);

            var referenceExpression = element as IReferenceExpression;
            var qualifier = referenceExpression?.QualifierExpression as IReferenceExpression;
            if (qualifier == null) return null;

            return ReferenceFromInvocation(qualifier, referenceExpression)
                   ?? ReferenceFromProperty(qualifier, referenceExpression);
        }

        private static IColorReference ReferenceFromConstructor(IObjectCreationExpression constructorExpression)
        {
            var constructedType = constructorExpression.TypeReference?.Resolve().DeclaredElement as ITypeElement;
            if (constructedType == null) return null;

            if (!IsUnityColorType(constructedType)) return null;

            var arguments = constructorExpression.Arguments;
            if (arguments.Count < 3 || arguments.Count > 4) return null;

            var baseColor = GetColorFromARGB(arguments);
            if (baseColor == null) return null;

            var color = baseColor.Item2;
            if (baseColor.Item1.HasValue)
                color = Color.FromArgb((int) (255.0 * baseColor.Item1.Value), baseColor.Item2);
            var colorElement = new ColorElement(color);
            var argumentList = constructorExpression.ArgumentList;
            return new UnityColorReference(colorElement, constructorExpression, argumentList, argumentList.GetDocumentRange());
        }

        private static IColorReference ReferenceFromInvocation(IReferenceExpression qualifier,
            IReferenceExpression methodReferenceExpression)
        {
            var invocationExpression = InvocationExpressionNavigator.GetByInvokedExpression(methodReferenceExpression);
            if (invocationExpression == null || invocationExpression.Arguments.IsEmpty)
            {
                return null;
            }

            var methodReference = methodReferenceExpression.Reference;

            var name = methodReference.GetName();
            if (!string.Equals(name, "HSVToRGB", StringComparison.Ordinal)) return null;

            var arguments = invocationExpression.Arguments;
            if (arguments.Count < 3 || arguments.Count > 4) return null;

            var color = GetColorFromHSV(arguments);
            if (color == null) return null;

            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;

            if (!IsUnityColorType(qualifierType)) return null;

            var colorElement = new ColorElement(color.Value);
            var argumentList = invocationExpression.ArgumentList;
            return new UnityColorReference(colorElement, invocationExpression,
                argumentList, argumentList.GetDocumentRange());
        }

        private static IColorReference ReferenceFromProperty(IReferenceExpression qualifier,
            IReferenceExpression colorQualifiedMemberExpression)
        {
            var name = colorQualifiedMemberExpression.Reference.GetName();

            var color = UnityNamedColors.Get(name);
            if (color == null) return null;

            var qualifierType = qualifier.Reference.Resolve().DeclaredElement as ITypeElement;
            if (qualifierType == null) return null;

            if (!IsUnityColorType(qualifierType)) return null;

            var property = colorQualifiedMemberExpression.Reference.Resolve().DeclaredElement as IProperty;
            if (property == null) return null;

            var colorElement = new ColorElement(color.Value, name);
            return new UnityColorReference(colorElement, colorQualifiedMemberExpression,
                 colorQualifiedMemberExpression, colorQualifiedMemberExpression.NameIdentifier.GetDocumentRange());
        }

        private static bool IsUnityColorType(ITypeElement typeElement)
        {
            return UnityColorTypes.GetInstance(typeElement.Module).IsUnityColorType(typeElement);
        }

        private static Tuple<float?, Color> GetColorFromARGB(ICollection<ICSharpArgument> arguments)
        {
            var a = GetArgumentAsFloatConstant(arguments, "a", 0, 1);
            var r = GetArgumentAsFloatConstant(arguments, "r", 0, 1);
            var g = GetArgumentAsFloatConstant(arguments, "g", 0, 1);
            var b = GetArgumentAsFloatConstant(arguments, "b", 0, 1);

            if (!r.HasValue || !g.HasValue || !b.HasValue)
                return null;

            return Tuple.Create(a, Color.FromArgb((int)(255.0 * r.Value), (int)(255.0 * g.Value), (int)(255.0 * b.Value)));
        }

        private static Color? GetColorFromHSV(ICollection<ICSharpArgument> arguments)
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
            if (constantValue.IsBadValue()) return null;

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

        private static ICSharpArgument GetNamedArgument(IEnumerable<ICSharpArgument> arguments, string parameterName)
        {
            var namedArgument =
                arguments.FirstOrDefault(
                    a =>
                        a.MatchingParameter.IfNotNull(
                            p => parameterName.Equals(p.Element.ShortName, StringComparison.Ordinal)));
            return namedArgument;
        }
    }
}