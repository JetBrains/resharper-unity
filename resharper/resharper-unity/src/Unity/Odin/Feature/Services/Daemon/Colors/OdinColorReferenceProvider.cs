#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Colors;
using JetBrains.ReSharper.Plugins.Unity.Odin.Attributes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util.Media;
using JetBrains.Util.Media.ColorSpaces;

namespace JetBrains.ReSharper.Plugins.Unity.Odin.Feature.Services.Daemon.Colors;

[SolutionComponent]
public class OdinColorReferenceProvider : IUnityColorReferenceProvider
{
    public IColorReference? GetColorReference(ITreeNode element)
    {
        if (element is not ICSharpArgument argument)
            return null;

        var attribute = AttributeNavigator.GetByArgument(argument);
        if (attribute == null)
            return null;

        var value = argument.Value;
        if (value == null)
            return null;
        if (!value.IsConstantValue())
            return null;

        var arguments = attribute.Arguments;

        if (arguments.IndexOf(argument) != 0)
            return null;
        
        var type = attribute.TypeReference?.Resolve().Result.DeclaredElement as ITypeElement;
        if (type == null)
            return null;

        if (!type.GetClrName().Equals(OdinKnownAttributes.GUIColorAttribute))
            return null;

        var constructor = attribute.ConstructorReference.Resolve().DeclaredElement as IConstructor;
        if (constructor == null)
            return null;


        // string
        if (constructor.Parameters.Count == 1)
        {
            var parameter = constructor.Parameters.First();
            if (!parameter.Type().IsString())
                return null;

            var stringValue = value.ConstantValue.AsString();
            if (stringValue == null)
                return null;

            // support hex only
            if (!stringValue.StartsWith("#"))
                return null;

            // TODO replace?
            var color = ColorSpace.HexToColor(stringValue.ToUpper());

            return new OdinHexColorReference(argument, argument.GetDocumentRange(), new ColorElement(color));
        }

        if (constructor.Parameters.Count == 4)
        {
            for (int i = 0; i < constructor.Parameters.Count; i++)
            {
                if (!constructor.Parameters[i].Type().IsFloat())
                    return null;
            }

            if (arguments.Count < 3)
                return null;

            var rArgument = arguments[0];
            var gArgument = arguments[1];
            var bArgument = arguments[2];

            var aArgument = arguments.Count == 4 ? arguments[3] : null;

            var color = JetRgbaColor.FromArgb(
                (byte)(255.0 * (UnityColorHighlighterProcess.ArgumentAsFloatConstant(0f, 1f, aArgument?.Value) ?? 1)),
                (byte)(255.0 * (UnityColorHighlighterProcess.ArgumentAsFloatConstant(0f, 1f, rArgument?.Value) ?? 0)),
                (byte)(255.0 * (UnityColorHighlighterProcess.ArgumentAsFloatConstant(0f, 1f, gArgument?.Value) ?? 0)),
                (byte)(255.0 * (UnityColorHighlighterProcess.ArgumentAsFloatConstant(0f, 1f, bArgument?.Value) ?? 0)));

            return new OdinColorReference(attribute, type, aArgument ?? bArgument, (aArgument ?? bArgument).GetDocumentRange(),
                new ColorElement(color), rArgument, gArgument, bArgument, aArgument);
        }

        return null;
    }


    private class OdinHexColorReference : IColorReference
    {
        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }

        public OdinHexColorReference(ITreeNode owner, DocumentRange documentRange, IColorElement colorElement)
        {
            Owner = owner;
            ColorConstantRange = documentRange;
            ColorElement = colorElement;
            BindOptions = new ColorBindOptions()
            {
                BindsToName = true,
                BindsToValue = true
            };
        }
        
        public void Bind(IColorElement colorElement)
        {
            using (WriteLockCookie.Create())
            {
                var factory = CSharpElementFactory.GetInstance(Owner);
                var literalExpression = factory.CreateStringLiteralExpression(ColorSpace.RGBToHex(colorElement.RGBColor).ToUpper());
                var argument = factory.CreateArgument(ParameterKind.VALUE, literalExpression);
                (Owner as ICSharpArgument).NotNull().ReplaceBy(argument);
            }
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return UnityNamedColors.GetColorTable();
        }

        public ColorBindOptions BindOptions { get; }
    }
    
    private class OdinColorReference : IColorReference
    {
        public ITreeNode Owner { get; }
        public DocumentRange? ColorConstantRange { get; }
        public IColorElement ColorElement { get; }

        private readonly IAttribute myAttribute;
        private readonly ITypeElement myAttributeType;
        private readonly ICSharpArgument myRArgument;
        private readonly ICSharpArgument myGArgument;
        private readonly ICSharpArgument myBArgument;
        private readonly ICSharpArgument myAArgument;
        
        public OdinColorReference(IAttribute attribute, ITypeElement attributeType, ITreeNode owner, DocumentRange documentRange, IColorElement colorElement,
            ICSharpArgument rArgument, ICSharpArgument gArgument, ICSharpArgument bArgument, ICSharpArgument aArgument)
        {
            Owner = owner;
            ColorConstantRange = documentRange;
            ColorElement = colorElement;
            BindOptions = new ColorBindOptions()
            {
                BindsToName = true,
                BindsToValue = true
            };

            myAttribute = attribute;
            myAttributeType = attributeType;
            myRArgument = rArgument;
            myGArgument = gArgument;
            myBArgument = bArgument;
            myAArgument = aArgument;
        }
        
        public void Bind(IColorElement colorElement)
        {
            using (WriteLockCookie.Create())
            {
                var factory = CSharpElementFactory.GetInstance(Owner);

                var newColor = colorElement.RGBColor;
                var module = myRArgument.GetPsiModule();
                
                var r = ConstantValue.Float((float) Math.Round(newColor.R / 255.0, 2), module);
                var g = ConstantValue.Float((float) Math.Round(newColor.G / 255.0, 2), module);
                var b = ConstantValue.Float((float) Math.Round(newColor.B / 255.0, 2), module);
                var a = ConstantValue.Float((float) Math.Round(newColor.A / 255.0, 2), module);

                var rExpression = factory.CreateExpressionByConstantValue(r);
                var gExpression = factory.CreateExpressionByConstantValue(g);
                var bExpression = factory.CreateExpressionByConstantValue(b);
                var aExpression = factory.CreateExpressionByConstantValue(a);

                var attribute = factory.CreateAttribute(myAttributeType);
                var rArgument = attribute.AddArgumentAfter(factory.CreateArgument(ParameterKind.VALUE, rExpression), null);
                var gArgument = attribute.AddArgumentAfter(factory.CreateArgument(ParameterKind.VALUE, gExpression), rArgument);
                var bArgument = attribute.AddArgumentAfter(factory.CreateArgument(ParameterKind.VALUE, bExpression), gArgument);

                if (newColor.A != 255)
                {
                    attribute.AddArgumentAfter(factory.CreateArgument(ParameterKind.VALUE, aExpression), bArgument);
                }

                LowLevelModificationUtil.ReplaceChild(myAttribute, attribute);
            }
        }

        public IEnumerable<IColorElement> GetColorTable()
        {
            return UnityNamedColors.GetColorTable();
        }

        public ColorBindOptions BindOptions { get; }
    }

}