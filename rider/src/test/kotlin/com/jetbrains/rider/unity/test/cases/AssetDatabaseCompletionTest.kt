package com.jetbrains.rider.unity.test.cases

import com.intellij.codeInsight.CodeInsightSettings
import com.intellij.codeInsight.editorActions.CompletionAutoPopupHandler
import com.intellij.testFramework.TestModeFlags
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.completion.RiderCodeCompletionExtraSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.ChecklistItems
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.annotations.Feature
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.annotations.Severity
import com.jetbrains.rider.test.annotations.SeverityLevel
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.env.enums.SdkVersion
import com.jetbrains.rider.test.facades.TestApiScopes
import com.jetbrains.rider.test.facades.editor.EditorApiFacade
import com.jetbrains.rider.test.facades.editor.RiderEditorApiFacade
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.persistAllFilesOnDisk
import com.jetbrains.rider.test.scriptingApi.*
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.testng.annotations.AfterMethod
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test
import java.io.File
import java.time.Duration

@Subsystem(SubsystemConstants.UNITY_COMPLETION)
@Feature("Unity Asset Database Autocompletion")
@Severity(SeverityLevel.NORMAL)
@TestEnvironment(sdkVersion = SdkVersion.DOT_NET_6)
class AssetDatabaseCompletionTest : BaseTestWithSolution(), TestApiScopes.Editor {
    override val editorApiFacade: EditorApiFacade by lazy { RiderEditorApiFacade(solutionApiFacade, testDataStorage) }

    override val testSolution: String = "AssetDatabasePathCompletionProject"

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
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "EmptyPathTest.cs") {
                typeWithLatency("\"")
                assertLookupContains(
                    "Assets\"",
                    "Packages\"",
                    checkFocus = false)
            }
        }
    }

    @Test(description = "Test not full path for asset database")
    @ChecklistItems(["Asset Database Completion/Not full path"])
    fun test_NotFullAssetsPathTest() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "NotFullAssetsPathTest.cs") {
                callBasicCompletion()
                assertLookupContains(
                    "Assets\"",
                    checkFocus = false)
            }
        }
    }

    @Test(description = "Test Assets folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Assets folder path"])
    fun test_AssetsFolderTest() {
        waitForUnityPackagesCache {
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
    }

    @Test(description = "Test Assets folder path for asset database with caret inside")
    @ChecklistItems(["Asset Database Completion/Assets folder path with caret inside"])
    fun test_AssetsFolderCaretInside() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "AssetsFolderTest.cs") {
                callBasicCompletion()
                assertLookupContains(
                    "Editor\"",
                    checkFocus = false)
            }
        }
    }

    @Test(description = "Test Assets internal folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Assets internal folder path"])
    fun test_AssetsInternalFolderTest() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "AssetsInternalFolderTest.cs") {
                typeWithLatency("/")
                assertLookupContains(
                    "from_res__EDITOR.bytes\"",
                    checkFocus = false)
            }
        }
    }

    @Test(description = "Test Package folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Package folder path"])
    fun test_PackagesFolderTest() {
        waitForUnityPackagesCache {
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
    }

    @Test(description = "Test Package internal folder path for asset database")
    @ChecklistItems(["Asset Database Completion/Package internal folder path"])
    fun test_PackagesInternalFolderTest() {
        waitForUnityPackagesCache {
            withOpenedEditor(File("Assets").resolve("EscapeFromRider.cs").path, "PackagesInternalFolderTest.cs") {
                typeWithLatency("/")
                assertLookupContains(
                    "Resources\"",
                    "Unity.com.jetbrains.from_git.asmdef\"",
                    checkFocus = false)
            }
        }
    }


    @BeforeMethod
    fun initializeEnvironment() {
        TestModeFlags.set(CompletionAutoPopupHandler.ourTestingAutopopup, true)

        CodeInsightSettings.getInstance().COMPLETION_CASE_SENSITIVE = CodeInsightSettings.NONE
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

    context(SolutionApiFacade)
    private fun waitForUnityPackagesCache(action: SolutionApiFacade.() -> Unit) {
        waitAndPump(project.lifetime,
                    { project.solution.frontendBackendModel.isUnityPackageManagerInitiallyIndexFinished.valueOrDefault(false) },
                    Duration.ofSeconds(10), { "Deferred caches are not completed" })
        action()
    }
}