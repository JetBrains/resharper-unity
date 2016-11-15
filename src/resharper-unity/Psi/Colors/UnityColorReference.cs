using System;
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Highlighting;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Colors
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

            BindOptions = new ColorBindOptions()
            {
                BindsToName = true,
                BindsToValue = true
            };
        }

        public void Bind(IColorElement colorElement)
        {
            if (TryReplaceAsNamed(colorElement))
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

        private bool TryReplaceAsNamed(IColorElement colorElement)
        {
            var newColor = UnityColorTypes.PropertyFromColorElement(colorElement, myOwningExpression.GetPsiModule());
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
            if (invocationExpression == null || invocationExpression.Arguments.Count < 3)
                return false;

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

            var module = myOwningExpression.GetPsiModule();
            var unityColorType = UnityColorTypes.GetInstance(module).UnityColorType;

            var elementFactory = CSharpElementFactory.GetInstance(Owner);

            // Round to 2 decimal places, to match the values shown in the colour palette quick fix
            var r = (float) Math.Round(newColor.R / 255.0, 2);
            var g = (float) Math.Round(newColor.G / 255.0, 2);
            var b = (float) Math.Round(newColor.B / 255.0, 2);
            var a = (float) Math.Round(newColor.A / 255.0, 2);

            ICSharpExpression newExp;
            if (newColor.A == byte.MaxValue)
            {
                newExp = elementFactory
                    .CreateExpression("new $0($1, $2, $3)", unityColorType,
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(r, module)),
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(g, module)),
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(b, module)));
            }
            else
            {
                newExp = elementFactory
                    .CreateExpression("new $0($1, $2, $3, $4)", unityColorType,
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(r, module)),
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(g, module)),
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(b, module)),
                        elementFactory.CreateExpressionByConstantValue(new ConstantValue(a, module)));
            }

            var oldExp = myOwningExpression as ICSharpExpression;
            oldExp?.ReplaceBy(newExp);
        }
    }
}