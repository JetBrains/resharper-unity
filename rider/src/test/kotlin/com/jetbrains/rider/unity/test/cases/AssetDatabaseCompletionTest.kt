package com.jetbrains.rider.unity.test.cases

import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.facades.TestApiScopes
import com.jetbrains.rider.test.facades.editor.EditorApiFacade
import com.jetbrains.rider.test.facades.editor.RiderEditorApiFacade
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.assertLookupContains
import com.jetbrains.rider.test.scriptingApi.callBasicCompletion
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.withOpenedEditor
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import com.jetbrains.rider.unity.test.framework.api.waitForUnityPackagesCache
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_COMPLETION)
@Feature("Unity Asset Database Autocompletion")
@Severity(SeverityLevel.NORMAL)
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("AssetDatabasePathCompletionProject")
class AssetDatabaseCompletionTest : PerTestSolutionTestBase(), TestApiScopes.Editor {
    override val editorApiFacade: EditorApiFacade by lazy { RiderEditorApiFacade(solutionApiFacade, testDataStorage) }

    override val traceCategories: List<String>
        get() = listOf(
            "#com.jetbrains.rdclient.completion",
            "#com.jetbrains.rdclient.document",
            "#com.jetbrains.rider.document",
            "#com.jetbrains.rider.editors",
            "#com.jetbrains.rider.completion",
            "#com.jetbrains.rdclient.editorActions",
            "JetBrains.ReSharper.Host.Features.Completion",
            "JetBrains.Rider.Test.Framework.Core.Documents",
            "JetBrains.ReSharper.Feature.Services.CodeCompletion",
            "JetBrains.ReSharper.Host.Features.Completion.Strategies.CSharp",
            "JetBrains.ReSharper.Host.Features.Documents",
            "JetBrains.ReSharper.Host.Features.TextControls",
            "JetBrains.ReSharper.Psi.Caches",
            "JetBrains.ReSharper.Psi.Files",
            "JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages")

    @Test(description = "Test empty path for asset database")
    @ChecklistItems(["Asset Database Completion/Empty path"])
    fun test_EmptyPath() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "EmptyPathTest.cs") {
            typeWithLatency("\"")
            assertLookupContains(
                "Assets\"",
                "Packages\"",
                checkFocus = false)
        }
    }

    @Test(description = "Test not full path for asset database")
    @ChecklistItems(["Asset Database Completion/Not full path"])
    fun test_NotFullAssetsPathTest() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "NotFullAssetsPathTest.cs") {
            callBasicCompletion()
            assertLookupContains(
                "Assets\"",
                checkFocus = false)
        }
    }

    @Test(description = "Test Assets folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Assets folder path"])
    fun test_AssetsFolderTest() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "AssetsFolderTest.cs") {
            typeWithLatency("/")
            assertLookupContains(
                "Editor\"",
                "Resources\"",
                "Scenes\"",
                "EscapeFromRider.cs\"",
                checkFocus = false)
        }
    }

    @Test(description = "Test Assets folder path for asset database with caret inside")
    @ChecklistItems(["Asset Database Completion/Assets folder path with caret inside"])
    fun test_AssetsFolderCaretInside() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "AssetsFolderTest.cs") {
            callBasicCompletion()
            assertLookupContains(
                "Editor\"",
                checkFocus = false)
        }
    }

    @Test(description = "Test Assets internal folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Assets internal folder path"])
    fun test_AssetsInternalFolderTest() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "AssetsInternalFolderTest.cs") {
            typeWithLatency("/")
            assertLookupContains(
                "from_res__EDITOR.bytes\"",
                checkFocus = false)
        }
    }

    @Test(description = "Test Package folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Package folder path"])
    fun test_PackagesFolderTest() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "PackagesFolderTest.cs") {
            typeWithLatency("/")
            assertLookupContains(
                "com.jetbrains.from_disk\"",
                "com.jetbrains.from_git\"",
                "com.jetbrains.from_pack_folder\"",
                "com.unity.ext.nunit\"",
                "com.unity.ide.rider\"",
                checkFocus = false)
        }
    }

    @Test(description = "Test Package internal folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Package internal folder path"])
    fun test_PackagesInternalFolderTest() {
        waitForUnityPackagesCache()
        withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "PackagesInternalFolderTest.cs") {
            typeWithLatency("/")
            assertLookupContains(
                "Resources\"",
                "Unity.com.jetbrains.from_git.asmdef\"",
                checkFocus = false)
        }
    }


    @BeforeMethod
    fun initializeEnvironment() {
        TestModeFlags.set(CompletionAutoPopupHandler.ourTestingAutopopup, true)

        CodeInsightSettings.getInstance().completionCaseSensitive = CodeInsightSettings.NONE
        CodeInsightSettings.getInstance().isSelectAutopopupSuggestionsByChars = true
        CodeInsightSettings.getInstance().AUTO_POPUP_JAVADOC_INFO = false

        //all tests were written with this setting which default was changed only in 18.3
        RiderCodeCompletionExtraSettings.instance.allowToCompleteWithWhitespace = true
        prepareAssemblies(project, activeSolutionDirectory)
    }

    // debug only
    @AfterMethod
    fun saveDocuments() {
        persistAllFilesOnDisk()
    }
}