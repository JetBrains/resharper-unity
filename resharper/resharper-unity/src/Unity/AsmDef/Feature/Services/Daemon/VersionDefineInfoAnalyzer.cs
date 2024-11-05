#nullable enable

using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.VersionUtils;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl.DocumentMarkup.Adornments;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    // Note that problem analysers for NonUserCode will still show Severity.INFO
    [ElementProblemAnalyzer(
        Instantiation.DemandAnyThreadSafe,
        typeof(IJsonNewLiteralExpression),
        HighlightingTypes = new[]
        {
            typeof(UnmetVersionConstraintInfo),
            typeof(PackageNotInstalledInfo),
            typeof(AsmDefPackageVersionInlayHintHighlighting),
            typeof(AsmDefPackageVersionInlayHintContextActionHighlighting)
        })]
    public class VersionDefineInfoAnalyzer : AsmDefProblemAnalyzer<IJsonNewLiteralExpression>
    {
        private readonly PackageManager myPackageManager;
        private readonly UnityVersion myUnityVersion;
        private readonly UnityExternalFilesPsiModule myExternalFilesPsiModule;

        public VersionDefineInfoAnalyzer(PackageManager packageManager,
                                         UnityExternalFilesModuleFactory externalFilesPsiModuleFactory,
                                         UnityVersion unityVersion)
        {
            myPackageManager = packageManager;
            myUnityVersion = unityVersion;
            myExternalFilesPsiModule = externalFilesPsiModuleFactory.PsiModule;
        }

        public override bool ShouldRun(IFile file, ElementProblemAnalyzerData data)
        {
            return base.ShouldRun(file, data) && IsProjectFileOrKnownExternalFile(data.SourceFile, myExternalFilesPsiModule);
        }

        protected override void Run(IJsonNewLiteralExpression element,
                                    ElementProblemAnalyzerData data,
                                    IHighlightingConsumer consumer)
        {
            if (element.IsVersionDefinesObjectDefineValue())
                AnalyzeDefineSymbol(element, consumer);
            else if (element.IsVersionDefinesObjectNameValue())
                AddNameInlayHints(element, data, consumer);
        }

        private void AnalyzeDefineSymbol(IJsonNewLiteralExpression element, IHighlightingConsumer consumer)
        {
            var value = element.GetUnquotedText();
            if (string.IsNullOrWhiteSpace(value)) return;

            var versionDefinesObject = GetVersionDefinesObject(element);
            var resourceName = versionDefinesObject.GetFirstPropertyValue<IJsonNewLiteralExpression>("name")?.GetStringValue();
            var expression = versionDefinesObject.GetFirstPropertyValue<IJsonNewLiteralExpression>("expression")?.GetStringValue();

            if (resourceName == null || expression == null || string.IsNullOrWhiteSpace(resourceName) || string.IsNullOrWhiteSpace(expression))
                return;

            if (!UnitySemanticVersionRange.TryParse(expression, out var range))
            {
                // TODO: Add highlight for invalid expression. Only useful for user code
                return;
            }

            UnitySemanticVersion? resourceVersion;

            var packageData = myPackageManager.GetPackageById(resourceName);
            if (packageData != null)
            {
                if (!UnitySemanticVersion.TryParse(packageData.PackageDetails.Version, out resourceVersion))
                    return;
            }
            else
            {
                if (resourceName == "Unity")
                {
                    var productVersion = UnityVersion.VersionToString(myUnityVersion.ActualVersionForSolution.Value);
                    if (!UnitySemanticVersion.TryParseProductVersion(productVersion, out resourceVersion))
                        return;
                }
                else
                {
                    consumer.AddHighlighting(new PackageNotInstalledInfo(element, resourceName));
                    return;
                }
            }

            if (!range.IsValid(resourceVersion))
                consumer.AddHighlighting(new UnmetVersionConstraintInfo(element, range.ToString()));
        }

        private void AddNameInlayHints(IJsonNewLiteralExpression element,
                                       ElementProblemAnalyzerData data,
                                       IHighlightingConsumer consumer)
        {
            var value = element.GetUnquotedText();
            if (string.IsNullOrWhiteSpace(value)) return;

            var packageVersionText = "";
            var packageData = myPackageManager.GetPackageById(value);
            if (packageData != null)
            {
                packageVersionText =
                    $"({packageData.PackageDetails.DisplayName}: {packageData.PackageDetails.Version})";
            }

            if (string.IsNullOrWhiteSpace(packageVersionText) && value == "Unity")
            {
                var productVersion = UnityVersion.VersionToString(myUnityVersion.ActualVersionForSolution.Value);
                packageVersionText = $"({productVersion})";
            }

            if (string.IsNullOrWhiteSpace(packageVersionText))
                return;

            var mode = ElementProblemAnalyzerUtils.GetInlayHintsMode(data,
                settings => settings.ShowAsmDefVersionDefinePackageVersions);
            if (mode != PushToHintMode.Never)
            {
                var documentOffset = element.GetDocumentEndOffset();

                // This highlight adds the inlay. It's always added but not always visible for push-to-hint
                consumer.AddHighlighting(
                    new AsmDefPackageVersionInlayHintHighlighting(documentOffset, packageVersionText, mode));

                // This highlight adds alt+enter context actions to configure the inlay. It's separate so that
                // we don't get alt+enter actions for an invisible push-to-hint inlay
                if (mode == PushToHintMode.Always)
                {
                    consumer.AddHighlighting(
                        new AsmDefPackageVersionInlayHintContextActionHighlighting(element.GetHighlightingRange()));
                }
            }
        }

        private static IJsonNewObject GetVersionDefinesObject(IJsonNewValue definePropertyValue)
        {
            // We've already checked structure
            var defineProperty = JsonNewMemberNavigator.GetByValue(definePropertyValue);
            return JsonNewObjectNavigator.GetByMember(defineProperty)!;
        }
    }
}