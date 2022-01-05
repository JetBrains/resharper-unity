using System;
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors
{
    public class UnityColorReference : IColorReference
    {
        private readonly IExpression myOwningExpression;

        public UnityColorReference(IColorElement colorElement, IExpression owningExpression, ITreeNode owner,
            DocumentRange colorConstantRange)
        {
            myOwningExpression = owningExpression;
            ColorElement = colorElement;
            Owner = owner;
            ColorConstantRange = colorConstantRange;

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
            float h, s, v;
            ColorUtils.ColorToHSV(newColor, out h, out s, out v);

            // Round to 2 decimal places to match the values shown in the colour palette quick fix
            h = (float) Math.Round(h, 2);
            s = (float) Math.Round(s, 2);
            v = (float) Math.Round(v, 2);

            var module = myOwningExpression.GetPsiModule();
            var elementFactory = CSharpElementFactory.GetInstance(Owner);

            // ReSharper disable AssignNullToNotNullAttribute
            var arguments = invocationExpression.Arguments;
            arguments[0].Value.ReplaceBy(elementFactory.CreateExpressionByConstantValue(new ConstantValue(h, module)));
            arguments[1].Value.ReplaceBy(elementFactory.CreateExpressionByConstantValue(new ConstantValue(s, module)));
            arguments[2].Value.ReplaceBy(elementFactory.CreateExpressionByConstantValue(new ConstantValue(v, module)));
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
                r = new ConstantValue((float) Math.Round(newColor.R / 255.0, 2), module);
                g = new ConstantValue((float) Math.Round(newColor.G / 255.0, 2), module);
                b = new ConstantValue((float) Math.Round(newColor.B / 255.0, 2), module);
                a = new ConstantValue((float) Math.Round(newColor.A / 255.0, 2), module);
            }
            else if (unityColorTypes.UnityColor32Type != null && unityColorTypes.UnityColor32Type.Equals(colorType))
            {
                // ReSharper formats byte constants with an explicit cast
                r = new ConstantValue((int)newColor.R, module);
                g = new ConstantValue((int)newColor.G, module);
                b = new ConstantValue((int)newColor.B, module);
                a = new ConstantValue((int)newColor.A, module);

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
            oldExp.ReplaceBy(newExp);
        }

        private ITypeElement GetColorType()
        {
            var referenceExpression = myOwningExpression as IReferenceExpression;
            var qualifier = referenceExpression?.QualifierExpression as IReferenceExpression;
            if (qualifier != null)
                return qualifier.Reference.Resolve().DeclaredElement as ITypeElement;

            var invocationExpression = myOwningExpression as IInvocationExpression;
            if (invocationExpression != null)
                return invocationExpression.Reference?.Resolve().DeclaredElement as ITypeElement;

            var objectCreationExpression = myOwningExpression as IObjectCreationExpression;
            return objectCreationExpression?.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        }
    }
}