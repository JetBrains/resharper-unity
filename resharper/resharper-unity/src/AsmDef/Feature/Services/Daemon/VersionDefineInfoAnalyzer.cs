using JetBrains.Application.InlayHints;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    // Note that problem analysers for NonUserCode will still show Severity.INFO
    [ElementProblemAnalyzer(typeof(IJsonNewLiteralExpression),
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
            myExternalFilesPsiModule = externalFilesPsiModuleFactory.PsiModule.NotNull("externalFilesPsiModuleFactory.PsiModule != null")!;
        }

        protected override void Analyze(IJsonNewLiteralExpression element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            // The source file must be either a project file, or a known external Unity file. Don't display anything
            // if the user opens an arbitrary .asmdef file
            var sourceFile = data.SourceFile;
            if (sourceFile == null ||
                (sourceFile.ToProjectFile() == null && !myExternalFilesPsiModule.ContainsFile(sourceFile)))
            {
                return;
            }

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

            if (!JetSemanticVersionRange.TryParse(expression, out var range))
            {
                // TODO: Add highlight for invalid expression. Only useful for user code
                return;
            }

            JetSemanticVersion resourceVersion;

            var packageData = myPackageManager.GetPackageById(resourceName);
            if (packageData != null)
            {
                if (!JetSemanticVersion.TryParse(packageData.PackageDetails.Version, out resourceVersion))
                    return;
            }
            else
            {
                if (resourceName == "Unity")
                    resourceVersion = new JetSemanticVersion(myUnityVersion.ActualVersionForSolution.Value);
                else
                {
                    consumer.AddHighlighting(new PackageNotInstalledInfo(element, resourceName));
                    return;
                }
            }

            if (packageData != null &&
                !JetSemanticVersion.TryParse(packageData.PackageDetails.Version, out resourceVersion))
            {
                return;
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
                packageVersionText = $"({myUnityVersion.ActualVersionForSolution.Value})";

            if (string.IsNullOrWhiteSpace(packageVersionText))
                return;

            var mode = GetMode(data, settings => settings.ShowAsmDefVersionDefinePackageVersions);
            if (mode != InlayHintsMode.Never)
            {
                var documentOffset = element.GetDocumentEndOffset();

                // This highlight adds the inlay. It's always added but not always visible for push-to-hint
                consumer.AddHighlighting(
                    new AsmDefPackageVersionInlayHintHighlighting(documentOffset, packageVersionText, mode));

                // This highlight adds alt+enter context actions to configure the inlay. It's separate so that
                // we don't get alt+enter actions for an invisible push-to-hint inlay
                if (mode == InlayHintsMode.Always)
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