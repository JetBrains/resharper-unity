using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.UsageChecking
{
    [ShellComponent]
    public class UsageInspectionsSuppressor : IUsageInspectionsSuppressor
    {
        private readonly ILogger myLogger;

        public UsageInspectionsSuppressor(ILogger logger)
        {
            myLogger = logger;
        }

        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            flags = ImplicitUseKindFlags.Default;

            try
            {
                if (!element.IsFromUnityProject()) return false;
            }
            catch (Exception e)
            {
                /*
                 * TODO: radically rethink Unity / non-Unity project detection.
                 * Currently we check project's assemblies using extensions for IProject,
                 * with no way to log errors and/or react to targetFrameworkChanges should they happen on the fly.                  
                 * This should be replaced with something more stable and fast.
                 */
                myLogger.LogExceptionSilently(e);
                return false;
            }

            var solution = element.GetSolution();
            var unityApi = solution.GetComponent<UnityApi>();

            switch (element)
            {
                case IClass cls when unityApi.IsUnityType(cls):
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case ITypeElement typeElement when unityApi.IsSerializableType(typeElement):
                    // TODO: We should only really mark it as in use if it's actually used somewhere
                    // That is, it should be used as a field in a Unity type, or another serializable type
                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                    return true;

                case IMethod method:
                    var function = unityApi.GetUnityEventFunction(method, out var match);
                    if (function != null)
                    {
                        if (match == MethodSignatureMatch.ExactMatch)
                        {
                            foreach (var parameter in function.Parameters)
                            {
                                if (parameter.IsOptional)
                                {
                                    // Allows optional parameters to be marked as unused
                                    // TODO: Might need to process IParameter if optional gets more complex
                                    flags = ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature;
                                    return true;
                                }
                            }

                            flags = ImplicitUseKindFlags.Access;
                            return true;
                        }

                        return false;
                    }

                    if (IsPotentialEventHandler(unityApi, method))
                    {
                        flags = ImplicitUseKindFlags.Access;
                        return true;
                    }

                    break;

                case IField field when unityApi.IsSerialisedField(field):
                    // Public fields gets exposed to the Unity Editor and assigned from the UI.
                    // But it still should be checked if the field is ever accessed from the code.
                    flags = ImplicitUseKindFlags.Assign;
                    return true;

                case IProperty property when IsPotentialEventHandler(unityApi, property.Setter):
                    flags = ImplicitUseKindFlags.Assign;
                    return true;
            }

            flags = ImplicitUseKindFlags.Default;   // Value not used if we return false
            return false;
        }

        // Best effort attempt at preventing false positives for type members that are actually being used inside a
        // scene. We don't have enough information to do this by name, so we'll mark all potential event handlers as
        // implicitly used by Unity
        // See https://github.com/Unity-Technologies/UnityCsReference/blob/02f8e8ca594f156dd6b2088ad89451143ca1b87e/Editor/Mono/Inspector/UnityEventDrawer.cs#L397
        private static bool IsPotentialEventHandler(UnityApi unityApi, [CanBeNull] IMethod method)
        {
            if (method == null)
                return false;

            if (!method.ReturnType.IsVoid())
                return false;

            // Type.GetMethods() returns public instance methods only
            if (method.GetAccessRights() != AccessRights.PUBLIC || method.IsStatic)
                return false;

            return unityApi.IsUnityType(method.GetContainingType()) && !method.HasAttributeInstance(PredefinedType.OBSOLETE_ATTRIBUTE_CLASS, true);
        }
    }
}
