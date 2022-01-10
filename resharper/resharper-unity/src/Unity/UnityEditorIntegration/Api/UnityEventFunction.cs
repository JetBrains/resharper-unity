using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.VersionUtils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api
{
    public class UnityEventFunction
    {
        private readonly Version myMinimumVersion;
        private readonly Version myMaximumVersion;

        public UnityEventFunction([NotNull] string name, [NotNull] IClrTypeName typeName,
                                  [NotNull] UnityTypeSpec returnType, bool isStatic, bool canBeCoroutine,
                                  string description, bool undocumented, Version minimumVersion,
                                  Version maximumVersion, [NotNull] params UnityEventFunctionParameter[] parameters)
        {
            Description = description;
            Undocumented = undocumented;
            IsStatic = isStatic;
            CanBeCoroutine = canBeCoroutine;
            myMinimumVersion = minimumVersion;
            myMaximumVersion = maximumVersion;
            Name = name;
            TypeName = typeName;
            ReturnType = returnType;
            Parameters = parameters.Length > 0 ? parameters : EmptyArray<UnityEventFunctionParameter>.Instance;
        }

        [NotNull] public IClrTypeName TypeName { get; }
        [NotNull] public string Name { get; }
        [NotNull] public UnityEventFunctionParameter[] Parameters { get; }
        [NotNull] public UnityTypeSpec ReturnType { get; }
        public bool CanBeCoroutine { get; }
        public bool IsStatic { get; }
        [CanBeNull] public string Description { get; }
        public bool Undocumented { get; }

        [NotNull]
        public IMethodDeclaration CreateDeclaration([NotNull] CSharpElementFactory factory,
                                                    [NotNull] KnownTypesCache knownTypesCache,
                                                    [NotNull] IClassLikeDeclaration classDeclaration,
                                                    AccessRights accessRights,
                                                    bool makeVirtual = false,
                                                    bool makeCoroutine = false)
        {
            var builder = new StringBuilder(128);
            var args = new object[1 + Parameters.Length];
            object arg;
            var argIndex = 0;
            var module = classDeclaration.GetPsiModule();

            if (accessRights != AccessRights.NONE)
            {
                builder.Append(CSharpDeclaredElementPresenter.Instance.Format(accessRights));
                builder.Append(" ");
            }

            if (IsStatic) builder.Append("static ");

            // Consider this declaration a template, and the final generated code implements (or overrides) this API
            if (makeVirtual) builder.Append("virtual ");
            if (makeCoroutine && CanBeCoroutine)
                builder.Append(PredefinedType.IENUMERATOR_FQN.FullName);
            else
            {
                arg = GetTypeObject(ReturnType, knownTypesCache, module);
                if (arg is string)
                {
                    builder.Append(arg);
                    if (ReturnType.IsArray) builder.Append("[]");
                }
                else
                {
                    builder.Append("$0");
                    args[argIndex++] = arg;
                }
            }

            builder.Append(" ");
            builder.Append(Name);
            builder.Append("(");

            for (var i = 0; i < Parameters.Length; i++)
            {
                if (i > 0) builder.Append(",");

                var parameter = Parameters[i];
                // TODO: `out` or `ref`?
                // From reflection point of view, it's a "ByRef" Type, and that's all we know...
                // The only place it's currently being used is an out parameter
                if (parameter.IsByRef) builder.Append("out ");

                arg = GetTypeObject(parameter.TypeSpec, knownTypesCache, module);
                if (arg is string)
                {
                    builder.Append(arg);
                    if (parameter.TypeSpec.IsArray) builder.Append("[]");
                }
                else
                {
                    builder.Append($"${argIndex}");
                    args[argIndex++] = arg;
                }
                builder.Append(' ');
                builder.Append(parameter.Name);
            }

            builder.Append(");");

            var declaration = (IMethodDeclaration) factory.CreateTypeMemberDeclaration(builder.ToString(), args);
            declaration.SetResolveContextForSandBox(classDeclaration, SandBoxContextType.Child);
            return declaration;
        }

        [CanBeNull]
        public UnityEventFunctionParameter GetParameter(string name)
        {
            return Parameters.FirstOrDefault(p => p.Name == name);
        }

        public bool SupportsVersion(Version unityVersion)
        {
            // Allow 2020.1 to also match 2020.1.4 for maximum version
            // CompareToIgnoringUndefinedComponents will also allow myMinimumVersion and myMaximumVersion to contain a
            // build and revision, e.g. an event function can be 2020.1.4 and correctly match a Unity version of
            // 2020.1.4 or even 2020.1.4.2000
            return myMinimumVersion.CompareToLenient(unityVersion) <= 0
                   && unityVersion.CompareToLenient(myMaximumVersion) <= 0;
        }

        private static object GetTypeObject(UnityTypeSpec typeSpec, KnownTypesCache knownTypesCache, IPsiModule module)
        {
            if (typeSpec.TypeParameters.Length == 0)
            {
                var keyword = CSharpTypeFactory.GetTypeKeyword(typeSpec.ClrTypeName);
                if (keyword != null)
                    return keyword;
            }

            return typeSpec.AsIType(knownTypesCache, module);
        }
    }
}