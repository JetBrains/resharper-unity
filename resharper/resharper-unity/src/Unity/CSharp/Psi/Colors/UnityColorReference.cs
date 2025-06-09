using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle.Suggestions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors
{
    public class UnityColorReference : IColorReference
    {
        private readonly IExpression myOwningExpression;
        private readonly IContextBoundSettingsStore mySettingsStore;

        public UnityColorReference(IColorElement colorElement, IExpression owningExpression, ITreeNode owner,
            DocumentRange colorConstantRange, IContextBoundSettingsStore settingsStore)
        {
            myOwningExpression = owningExpression;
            ColorElement = colorElement;
            Owner = owner;
            ColorConstantRange = colorConstantRange;
            mySettingsStore = settingsStore;

            BindOptions = new ColorBindOptions
            {
                BindsToName = true,
                BindsToValue = true
            };
        }

        public void Bind(IColorElement colorElement)
        {
            if (TryReplaceAsNamedColor(colorElement))
                return;

            if (TryReplaceAsHSV(colorElement))
                return;

            TryReplaceAsConstructor(colorElement);
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return UnityNamedColors.GetColorTable();
        }

        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }
        public ColorBindOptions BindOptions { get; }

        private bool TryReplaceAsNamedColor(IColorElement colorElement)
        {
            var colorType = GetColorType();

            var newColor = UnityColorTypes.PropertyFromColorElement(colorType, colorElement,
                myOwningExpression.GetPsiModule());
            if (newColor == null) return false;

            var newExp = CSharpElementFactory.GetInstance(Owner)
                .CreateExpression("$0.$1", newColor.Value.First, newColor.Value.Second);

            var oldExp = myOwningExpression as ICSharpExpression;
            return oldExp?.ReplaceBy(newExp) != null;
        }

        private bool TryReplaceAsHSV(IColorElement colorElement)
        {
            // Only do this if we've already got a call to HSVToRGB
            var invocationExpression = myOwningExpression as IInvocationExpression;
            if (invocationExpression == null || invocationExpression.Reference?.GetName() != "HSVToRGB" ||
                invocationExpression.Arguments.Count < 3)
            {
                return false;
            }

            var newColor = colorElement.RGBColor;
            ColorUtils.ColorToHSV(newColor, out var h, out var s, out var v);

            // Round to 2 decimal places to match the values shown in the colour palette quick fix
            h = (float) Math.Round(h, 2);
            s = (float) Math.Round(s, 2);
            v = (float) Math.Round(v, 2);

            var module = myOwningExpression.GetPsiModule();
            var elementFactory = CSharpElementFactory.GetInstance(Owner);

            // ReSharper disable AssignNullToNotNullAttribute
            var arguments = invocationExpression.Arguments;
            arguments[0].Value.ReplaceBy(elementFactory.CreateExpressionByConstantValue(ConstantValue.Float(h, module)));
            arguments[1].Value.ReplaceBy(elementFactory.CreateExpressionByConstantValue(ConstantValue.Float(s, module)));
            arguments[2].Value.ReplaceBy(elementFactory.CreateExpressionByConstantValue(ConstantValue.Float(v, module)));
            // ReSharper restore AssignNullToNotNullAttribute

            return true;
        }

        private void TryReplaceAsConstructor(IColorElement colorElement)
        {
            // TODO: Perhaps we should try and update an existing constructor?
            var newColor = colorElement.RGBColor;

            var colorType = GetColorType();
            if (colorType == null) return;

            var elementFactory = CSharpElementFactory.GetInstance(Owner);
            var module = myOwningExpression.GetPsiModule();
            var unityColorTypes = UnityColorTypes.GetInstance(module);

            var requiresAlpha = newColor.A != byte.MaxValue;

            ConstantValue r, g, b, a;
            if (unityColorTypes.UnityColorType != null && unityColorTypes.UnityColorType.Equals(colorType))
            {
                // Round to 2 decimal places, to match the values shown in the colour palette quick fix
                r = ConstantValue.Float((float) Math.Round(newColor.R / 255.0, 2), module);
                g = ConstantValue.Float((float) Math.Round(newColor.G / 255.0, 2), module);
                b = ConstantValue.Float((float) Math.Round(newColor.B / 255.0, 2), module);
                a = ConstantValue.Float((float) Math.Round(newColor.A / 255.0, 2), module);
            }
            else if (unityColorTypes.UnityColor32Type != null && unityColorTypes.UnityColor32Type.Equals(colorType))
            {
                // ReSharper formats byte constants with an explicit cast
                r = ConstantValue.Int(newColor.R, module);
                g = ConstantValue.Int(newColor.G, module);
                b = ConstantValue.Int(newColor.B, module);
                a = ConstantValue.Int(newColor.A, module);

                requiresAlpha = true;
            }
            else
                return;

            ICSharpExpression newExp;
            if (!requiresAlpha)
            {
                newExp = elementFactory
                    .CreateExpression("new $0($1, $2, $3)", colorType,
                        elementFactory.CreateExpressionByConstantValue(r),
                        elementFactory.CreateExpressionByConstantValue(g),
                        elementFactory.CreateExpressionByConstantValue(b));
            }
            else
            {
                newExp = elementFactory
                    .CreateExpression("new $0($1, $2, $3, $4)", colorType,
                        elementFactory.CreateExpressionByConstantValue(r),
                        elementFactory.CreateExpressionByConstantValue(g),
                        elementFactory.CreateExpressionByConstantValue(b),
                        elementFactory.CreateExpressionByConstantValue(a));
            }

            var oldExp = (ICSharpExpression) myOwningExpression;
            
            var result = oldExp.ReplaceBy(newExp);
            CodeStyleUtil.ApplyStyle<IObjectCreationStyleSuggestion>(result, mySettingsStore);
        }

        private ITypeElement GetColorType()
        {
            var referenceExpression = myOwningExpression as IReferenceExpression;
            if (referenceExpression?.QualifierExpression is IReferenceExpression qualifier)
                return qualifier.Reference.Resolve().DeclaredElement as ITypeElement;

            if (myOwningExpression is IInvocationExpression invocationExpression)
                return invocationExpression.Reference.Resolve().DeclaredElement as ITypeElement;

            var objectCreationExpression = myOwningExpression as IObjectCreationExpression;
            // Handle explicit `new T(...)`
            if (objectCreationExpression?.TypeReference?.Resolve().DeclaredElement is ITypeElement explicitTypeElement) 
                return explicitTypeElement;
            
            // Handle target-typed `new(...)`
            var typeMember = objectCreationExpression?.ConstructorReference.Resolve().DeclaredElement as ITypeMember;
            return typeMember?.ContainingType;
        }
    }
}